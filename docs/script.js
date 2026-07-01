// Smooth scroll to section
function scrollToSection(sectionId) {
    const section = document.getElementById(sectionId);
    if (section) {
        section.scrollIntoView({ behavior: 'smooth' });
    }
}

// Purchase via Stripe
function purchaseStriped(plan) {
    // TODO: Integrate with Stripe Checkout
    // For now, show alert
    alert(`Purchase flow for ${plan} plan - Stripe integration coming soon!`);

    // Example Stripe integration (uncomment when ready):
    /*
    const stripe = Stripe('pk_live_YOUR_KEY');

    const planPrices = {
        'store': 'price_1234567890',      // $9.99
        'direct': 'price_0987654321'      // $7.99
    };

    stripe.redirectToCheckout({
        lineItems: [{ price: planPrices[plan], quantity: 1 }],
        mode: 'payment',
        successUrl: `${window.location.origin}/success.html?plan=${plan}`,
        cancelUrl: `${window.location.origin}/`
    });
    */
}

// Smooth animations on scroll
const observerOptions = {
    threshold: 0.1,
    rootMargin: '0px 0px -100px 0px'
};

const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            entry.target.style.opacity = '1';
            entry.target.style.transform = 'translateY(0)';
        }
    });
}, observerOptions);

// Observe feature cards
document.querySelectorAll('.feature-card, .arch-card, .pricing-card').forEach(el => {
    el.style.opacity = '0';
    el.style.transform = 'translateY(20px)';
    el.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
    observer.observe(el);
});

// Dark mode toggle (optional)
function toggleDarkMode() {
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        document.documentElement.style.colorScheme = 'light';
    } else {
        document.documentElement.style.colorScheme = 'dark';
    }
}

// Log page load
console.log('Glance Landing Page loaded');
