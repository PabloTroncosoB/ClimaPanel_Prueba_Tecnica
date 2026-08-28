document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("form[data-confirm]").forEach(form => {
        form.addEventListener("submit", event => {
            const message = form.getAttribute("data-confirm") || "¿Continuar?";
            if (!window.confirm(message)) {
                event.preventDefault();
            }
        });
    });

    document.querySelectorAll("form[data-lock-submit]").forEach(form => {
        form.addEventListener("submit", () => {
            const button = form.querySelector("button[type='submit']");
            if (button) {
                button.disabled = true;
                button.textContent = "Procesando…";
            }
        });
    });
});
