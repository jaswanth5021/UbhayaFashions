(() => {
    const host = document.getElementById('shippingAddressAutocomplete');
    const addressField = document.getElementById('shippingAddress');
    const apiKey = host?.dataset.googleMapsKey?.trim();
    if (!host || !addressField || !apiKey) return;

    window.initShippingAddressAutocomplete = async () => {
        try {
            const { PlaceAutocompleteElement } = await google.maps.importLibrary('places');
            const autocomplete = new PlaceAutocompleteElement({
                includedRegionCodes: ['in']
            });
            autocomplete.setAttribute('aria-label', 'Search delivery address');
            autocomplete.placeholder = 'Start typing your address';
            host.appendChild(autocomplete);

            autocomplete.addEventListener('gmp-select', async ({ placePrediction }) => {
                try {
                    const place = placePrediction.toPlace();
                    await place.fetchFields({ fields: ['formattedAddress'] });
                    if (place.formattedAddress) {
                        addressField.value = place.formattedAddress;
                        addressField.dispatchEvent(new Event('input', { bubbles: true }));
                    }
                } catch (error) {
                    console.error('Could not retrieve the selected address.', error);
                }
            });
        } catch (error) {
            console.error('Google address autocomplete could not be initialized.', error);
        }
    };

    const script = document.createElement('script');
    script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(apiKey)}&v=weekly&loading=async&callback=initShippingAddressAutocomplete`;
    script.async = true;
    script.onerror = () => console.error('Google Maps JavaScript API could not be loaded.');
    document.head.appendChild(script);
})();
