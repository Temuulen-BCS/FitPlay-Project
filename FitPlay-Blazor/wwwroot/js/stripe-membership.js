window.fitplayStripe = (() => {
    let stripe = null;
    let elements = null;
    let paymentElement = null;

    const init = () => {
        const publishableKey = document.querySelector("meta[name='stripe-pk']")?.getAttribute("content");
        if (!publishableKey) {
            throw new Error("Stripe publishable key not configured.");
        }
        if (!stripe) {
            stripe = Stripe(publishableKey);
        }
    };

    const mount = async (clientSecret, elementSelector) => {
        try {
            init();
            const selector = elementSelector || "#payment-element";

            const container = document.querySelector(selector);
            if (!container) {
                return "Payment form container not found. Please try again.";
            }

            // Destroy previous elements if they exist
            if (paymentElement) {
                try { paymentElement.destroy(); } catch (_) { }
                paymentElement = null;
            }

            elements = stripe.elements({ clientSecret });
            paymentElement = elements.create("payment");
            paymentElement.mount(selector);
            return null; // success
        } catch (error) {
            console.error("Stripe mount error:", error);
            return error.message || "Failed to load payment form.";
        }
    };

    const confirm = async (returnUrl) => {
        if (!stripe || !elements) {
            return "Payment is not ready yet.";
        }

        const { error } = await stripe.confirmPayment({
            elements,
            confirmParams: {
                return_url: returnUrl
            },
            redirect: "if_required"
        });

        return error ? error.message : null;
    };

    return { mount, confirm };
})();
