(() => {
    const drawer = document.getElementById('cartDrawer');
    const addressStep = document.getElementById('cartDrawerAddressStep');
    if (!drawer || !addressStep) return;

    const list = document.getElementById('drawerAddressList');
    const addButton = document.getElementById('drawerAddNewAddress');
    const newAddress = document.getElementById('drawerNewAddress');
    const cancelNew = document.getElementById('drawerCancelNewAddress');
    const useLocation = document.getElementById('drawerUseLocation');
    const saveButton = document.getElementById('drawerSaveAddress');
    const selected = document.getElementById('drawerSelectedAddress');
    const shipping = document.getElementById('drawerShippingAddress');
    const continueButton = document.getElementById('drawerContinuePayment');
    const error = document.getElementById('drawerAddressError');
    const autocompleteHost = document.getElementById('drawerAddressAutocomplete');
    const googleKey = autocompleteHost?.dataset.googleMapsKey?.trim();
    const csrf = document.querySelector('#drawerCheckoutForm input[name="__RequestVerificationToken"]')?.value;

    const field = id => document.getElementById(id);
    const setError = message => {
        if (!error) return;
        error.textContent = message || '';
        error.hidden = !message;
    };

    const getSelectedOption = () => document.querySelector('input[name="drawerSavedAddress"]:checked');

    const updateSelected = option => {
        if (!option) {
            shipping.value = '';
            selected.hidden = true;
            selected.textContent = '';
            continueButton.disabled = true;
            return;
        }
        shipping.value = option.dataset.address || '';
        selected.textContent = `${option.dataset.name || 'Address'} · ${option.dataset.address || ''}`;
        selected.hidden = false;
        continueButton.disabled = !shipping.value.trim();
    };

    const showNewAddress = show => {
        newAddress.hidden = !show;
        if (show) {
            setError('');
            field('newAddressName').focus();
        }
    };

    document.querySelectorAll('input[name="drawerSavedAddress"]').forEach(option => {
        option.addEventListener('change', () => updateSelected(option));
    });

    addButton?.addEventListener('click', () => {
        document.querySelectorAll('input[name="drawerSavedAddress"]').forEach(x => x.checked = false);
        shipping.value = '';
        selected.hidden = true;
        continueButton.disabled = true;
        showNewAddress(true);
    });

    cancelNew?.addEventListener('click', () => showNewAddress(false));

    document.getElementById('cartDrawerAddressClose')?.addEventListener('click', () => {
        document.getElementById('cartDrawerClose')?.click();
    });

    const populateFromAddress = address => {
        if (!address) return;
        field('newAddressLine1').value = [address.houseNumber, address.road].filter(Boolean).join(', ') || address.display_name?.split(',')[0] || '';
        field('newAddressLine2').value = [address.suburb, address.neighbourhood, address.quarter, address.landmark].filter(Boolean).join(', ');
        field('newAddressCity').value = address.city || address.town || address.village || address.municipality || '';
        field('newAddressState').value = address.state || '';
        field('newAddressPostalCode').value = address.postcode || '';
    };

    const reverseGeocode = async (lat, lon) => {
        // If Google Maps is configured, prefer Google's geocoder.
        if (window.google?.maps?.Geocoder) {
            return new Promise((resolve, reject) => {
                new google.maps.Geocoder().geocode({ location: { lat, lng: lon } }, (results, status) => {
                    if (status === 'OK' && results?.[0]) {
                        const components = {};
                        results[0].address_components.forEach(c => c.types.forEach(t => components[t] = c.long_name));
                        resolve({
                            houseNumber: components.street_number || '',
                            road: components.route || '',
                            suburb: components.sublocality_level_1 || components.sublocality || '',
                            city: components.locality || components.administrative_area_level_2 || '',
                            state: components.administrative_area_level_1 || '',
                            postcode: components.postal_code || '',
                            display_name: results[0].formatted_address || ''
                        });
                    } else reject(new Error('Could not convert your location into an address.'));
                });
            });
        }

        const response = await fetch(`/Addresses/ReverseGeocode?latitude=${encodeURIComponent(lat)}&longitude=${encodeURIComponent(lon)}`, { credentials: 'same-origin' });
        const data = await response.json();
        if (!response.ok || !data.success) throw new Error(data.message || 'Could not find your address.');
        return data.address;
    };

    useLocation?.addEventListener('click', () => {
        if (!navigator.geolocation) {
            setError('Location is not supported by this browser.');
            return;
        }
        setError('');
        useLocation.disabled = true;
        useLocation.innerHTML = '<span aria-hidden="true">⌖</span> Finding your address…';
        navigator.geolocation.getCurrentPosition(async position => {
            try {
                const address = await reverseGeocode(position.coords.latitude, position.coords.longitude);
                populateFromAddress(address);
            } catch (e) {
                setError(e.message || 'Could not load your address.');
            } finally {
                useLocation.disabled = false;
                useLocation.innerHTML = '<span aria-hidden="true">⌖</span> Use my current location';
            }
        }, () => {
            setError('Location permission was not granted. You can search your address instead.');
            useLocation.disabled = false;
            useLocation.innerHTML = '<span aria-hidden="true">⌖</span> Use my current location';
        }, { enableHighAccuracy: true, timeout: 12000, maximumAge: 300000 });
    });

    // Match the shopping-site experience: when there is no saved address,
    // try to load the customer's current address as soon as checkout opens.
    let locationAttempted = false;
    document.getElementById('cartDrawerBeginCheckout')?.addEventListener('click', () => {
        const hasSavedAddress = !!getSelectedOption();
        if (!hasSavedAddress && !locationAttempted && !newAddress.hidden) return;
        if (!hasSavedAddress && !locationAttempted) {
            locationAttempted = true;
            showNewAddress(true);
            useLocation?.click();
        }
    });

    const clearNewFields = () => {
        ['newAddressName','newAddressMobile','newAddressLine1','newAddressLine2','newAddressCity','newAddressState','newAddressPostalCode'].forEach(id => field(id).value = '');
        field('newAddressDefault').checked = false;
    };

    const createAddressCard = address => {
        const label = document.createElement('label');
        label.className = 'drawer-address-card';
        label.innerHTML = `
            <input type="radio" name="drawerSavedAddress" value="${address.id}" data-address="${escapeHtml(address.fullAddress)}" data-name="${escapeHtml(address.name)}" data-mobile="${escapeHtml(address.mobile)}">
            <span class="drawer-address-radio" aria-hidden="true"></span>
            <span class="drawer-address-card-copy"><strong>${escapeHtml(address.name)}</strong><span>${escapeHtml(address.fullAddress)}</span><small>${escapeHtml(address.mobile)}</small></span>
            <button type="button" class="drawer-address-delete" data-address-id="${address.id}" aria-label="Delete address">×</button>`;
        return label;
    };

    const escapeHtml = value => String(value ?? '').replace(/[&<>'"]/g, c => ({ '&':'&amp;', '<':'&lt;', '>':'&gt;', "'":'&#39;', '"':'&quot;' }[c]));

    saveButton?.addEventListener('click', async () => {
        const model = {
            name: field('newAddressName').value.trim(),
            mobile: field('newAddressMobile').value.trim(),
            addressLine1: field('newAddressLine1').value.trim(),
            addressLine2: field('newAddressLine2').value.trim(),
            city: field('newAddressCity').value.trim(),
            state: field('newAddressState').value.trim(),
            postalCode: field('newAddressPostalCode').value.trim(),
            country: 'India',
            isDefault: field('newAddressDefault').checked
        };
        if (!model.name || !model.mobile || !model.addressLine1 || !model.city || !model.state || !model.postalCode) {
            setError('Please complete name, mobile, address, city, state and PIN code.');
            return;
        }

        setError('');
        saveButton.disabled = true;
        saveButton.textContent = 'Saving…';
        try {
            const form = new FormData();
            Object.entries(model).forEach(([key, value]) => form.append(key, value));
            form.append('__RequestVerificationToken', csrf || '');
            const response = await fetch('/Addresses/Save', { method: 'POST', body: form, credentials: 'same-origin' });
            const result = await response.json();
            if (!response.ok || !result.success) throw new Error(result.message || 'Could not save address.');

            const address = result.address;
            if (model.isDefault) document.querySelectorAll('input[name="drawerSavedAddress"]').forEach(x => x.checked = false);
            const card = createAddressCard(address);
            list.querySelector('.drawer-no-address')?.remove();
            list.prepend(card);
            const option = card.querySelector('input');
            option.checked = true;
            option.addEventListener('change', () => updateSelected(option));
            card.querySelector('.drawer-address-delete')?.addEventListener('click', deleteAddress);
            updateSelected(option);
            clearNewFields();
            showNewAddress(false);
        } catch (e) {
            setError(e.message || 'Could not save address.');
        } finally {
            saveButton.disabled = false;
            saveButton.textContent = 'Save address';
        }
    });

    async function deleteAddress(event) {
        event.preventDefault();
        event.stopPropagation();
        const button = event.currentTarget;
        const id = button.dataset.addressId;
        if (!id || !confirm('Remove this address?')) return;
        const form = new FormData();
        form.append('id', id);
        form.append('__RequestVerificationToken', csrf || '');
        const response = await fetch('/Addresses/Delete', { method: 'POST', body: form, credentials: 'same-origin' });
        const result = await response.json();
        if (!response.ok || !result.success) { setError(result.message || 'Could not remove address.'); return; }
        const card = button.closest('.drawer-address-card');
        const wasSelected = card.querySelector('input')?.checked;
        card.remove();
        if (wasSelected) {
            const first = list.querySelector('input[name="drawerSavedAddress"]');
            if (first) { first.checked = true; updateSelected(first); } else { updateSelected(null); }
        }
    }

    list?.querySelectorAll('.drawer-address-delete').forEach(button => button.addEventListener('click', deleteAddress));

    const first = getSelectedOption();
    if (first) updateSelected(first);
    else continueButton.disabled = true;

    const initGoogle = async () => {
        if (!googleKey || !autocompleteHost) return;
        try {
            if (!window.google?.maps?.importLibrary) {
                await new Promise((resolve, reject) => {
                    const script = document.createElement('script');
                    script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(googleKey)}&v=weekly&loading=async`;
                    script.async = true;
                    script.onload = resolve;
                    script.onerror = reject;
                    document.head.appendChild(script);
                });
            }
            const { PlaceAutocompleteElement } = await google.maps.importLibrary('places');
            const autocomplete = new PlaceAutocompleteElement({ includedRegionCodes: ['in'] });
            autocomplete.placeholder = 'Search your address';
            autocomplete.addEventListener('gmp-select', async ({ placePrediction }) => {
                const place = placePrediction.toPlace();
                await place.fetchFields({ fields: ['formattedAddress', 'addressComponents'] });
                const components = {};
                (place.addressComponents || []).forEach(c => (c.types || []).forEach(t => components[t] = c.longText || c.shortText || ''));
                field('newAddressLine1').value = [components.street_number, components.route].filter(Boolean).join(', ');
                field('newAddressLine2').value = [components.sublocality_level_1, components.sublocality].filter(Boolean).join(', ');
                field('newAddressCity').value = components.locality || components.administrative_area_level_2 || '';
                field('newAddressState').value = components.administrative_area_level_1 || '';
                field('newAddressPostalCode').value = components.postal_code || '';
            });
            autocompleteHost.appendChild(autocomplete);
        } catch (e) { console.warn('Google address autocomplete unavailable.', e); }
    };
    initGoogle();
})();
