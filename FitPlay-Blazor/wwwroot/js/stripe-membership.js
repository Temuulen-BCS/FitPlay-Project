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

    const mount = async (clientSecret) => {
        init();
        elements = stripe.elements({ clientSecret });
        paymentElement = elements.create("payment");
        paymentElement.mount("#payment-element");
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
