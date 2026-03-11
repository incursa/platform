(function () {
  function initializePasswordToggles() {
    document.querySelectorAll("[data-rr-password-toggle]").forEach(function (button) {
      button.addEventListener("click", function () {
        var row = button.closest(".rr-auth-input-row");
        if (!row) {
          return;
        }

        var input = row.querySelector("input");
        if (!input) {
          return;
        }

        var isPassword = input.getAttribute("type") === "password";
        input.setAttribute("type", isPassword ? "text" : "password");
        button.textContent = isPassword ? "Hide" : "Show";
      });
    });
  }

  initializePasswordToggles();
})();
