document.addEventListener('DOMContentLoaded', function() {
    const seatInput = document.getElementById("seatInput");
    const seatCount = document.getElementById("seatCount");
    const totalAmount = document.getElementById("totalAmount");
    const pricePerSeat = parseFloat(document.getElementById("pricePerSeat")?.innerText) || 0;
    const form = document.getElementById("bookingForm");
    const confirmBtn = document.getElementById("confirmBtn");

    function updateTotal() {
        const seats = parseInt(seatInput?.value) || 1;
        if (seatCount) seatCount.innerText = seats;
        if (totalAmount) totalAmount.innerText = (seats * pricePerSeat).toFixed(2);
    }

    function validateEmail(email) {
        // Proper email validation using regex
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email.trim());
    }

    function validatePhoneNumber(phone) {
        // Remove all non-digits
        const cleanPhone = phone.replace(/\D/g, '');
        // Check if it's between 10-15 digits
        return cleanPhone.length >= 10 && cleanPhone.length <= 15;
    }

    if (seatInput) {
        seatInput.addEventListener("input", updateTotal);
        seatInput.addEventListener("change", updateTotal);
    }
    
    if (form) {
        form.addEventListener('submit', function(e) {
            const customerName = document.querySelector('input[name="CustomerName"]')?.value.trim() || '';
            const email = document.querySelector('input[name="Email"]')?.value.trim() || '';
            const phone = document.querySelector('input[name="PhoneNumber"]')?.value.trim() || '';
            const seats = parseInt(document.querySelector('input[name="NumberOfSeats"]')?.value) || 0;

            console.log('Submitting booking:', { customerName, email, phone, seats });

            let hasErrors = false;
            let errorMessages = [];

            // Validate customer name
            if (!customerName || customerName.length < 2) {
                errorMessages.push('Please enter a valid name (at least 2 characters)');
                hasErrors = true;
            }

            // Validate email
            if (!email) {
                errorMessages.push('Email address is required');
                hasErrors = true;
            } else if (!validateEmail(email)) {
                errorMessages.push('Please enter a valid email address (e.g., user@example.com)');
                hasErrors = true;
            }

            // Validate phone number
            if (!phone) {
                errorMessages.push('Phone number is required');
                hasErrors = true;
            } else if (!validatePhoneNumber(phone)) {
                errorMessages.push('Please enter a valid phone number (10-15 digits)');
                hasErrors = true;
            }

            // Validate seats
            if (!seats || seats < 1 || seats > 10) {
                errorMessages.push('Please select between 1 and 10 seats');
                hasErrors = true;
            }

            if (hasErrors) {
                alert('Please fix the following errors:\n\n' + errorMessages.join('\n'));
                e.preventDefault();
                return false;
            }

            // If all validations pass, disable the submit button
            if (confirmBtn) {
                confirmBtn.disabled = true;
                confirmBtn.innerHTML = '<i class="bi bi-hourglass-split"></i> Processing...';
            }

            return true;
        });
    }

    // Real-time email validation feedback
    const emailInput = document.querySelector('input[name="Email"]');
    if (emailInput) {
        emailInput.addEventListener('blur', function() {
            const email = this.value.trim();
            if (email && !validateEmail(email)) {
                this.classList.add('is-invalid');
                
                // Add or update error message
                let errorSpan = this.parentNode.querySelector('.email-error');
                if (!errorSpan) {
                    errorSpan = document.createElement('span');
                    errorSpan.className = 'text-danger email-error';
                    this.parentNode.appendChild(errorSpan);
                }
                errorSpan.textContent = 'Please enter a valid email address';
            } else {
                this.classList.remove('is-invalid');
                const errorSpan = this.parentNode.querySelector('.email-error');
                if (errorSpan) {
                    errorSpan.remove();
                }
            }
        });

        emailInput.addEventListener('input', function() {
            // Clear validation styling while typing
            this.classList.remove('is-invalid');
            const errorSpan = this.parentNode.querySelector('.email-error');
            if (errorSpan) {
                errorSpan.remove();
            }
        });
    }

    // Phone number formatting
    const phoneInput = document.querySelector('input[name="PhoneNumber"]');
    if (phoneInput) {
        phoneInput.addEventListener('input', function(e) {
            // Remove all non-digits
            let value = e.target.value.replace(/\D/g, '');
            
            // Limit to 15 digits
            if (value.length > 15) {
                value = value.substring(0, 15);
            }
            
            e.target.value = value;
        });

        phoneInput.addEventListener('blur', function() {
            const phone = this.value.trim();
            if (phone && !validatePhoneNumber(phone)) {
                this.classList.add('is-invalid');
            } else {
                this.classList.remove('is-invalid');
            }
        });
    }
    
    updateTotal();
});