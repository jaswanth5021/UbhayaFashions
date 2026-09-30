(() => {
    const addressField = document.getElementById('shippingAddress');
    const addNewButton = document.getElementById('addNewAddress');
    const addressOptions = [...document.querySelectorAll('input[name="savedAddressChoice"]')];
    if (!addressField) return;

    const selectedAddress = addressOptions.find(option => option.checked);
    if (selectedAddress && !addressField.value) {
        addressField.value = selectedAddress.dataset.address || '';
    }

    addressOptions.forEach(option => {
        option.addEventListener('change', () => {
            addressField.value = option.dataset.address || '';
        });
    });

    addNewButton?.addEventListener('click', () => {
        addressOptions.forEach(option => { option.checked = false; });
        addressField.value = '';
        addressField.focus();
    });

    addressField.addEventListener('input', () => {
        addressOptions.forEach(option => { option.checked = false; });
    });
})();
