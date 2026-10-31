/* Page chrome only: the appearance switch. The linked-views wiring is linked-views.js. */
(function () {
  "use strict";
  var root = document.documentElement, KEY = "cfx-example-theme";
  function apply(theme) {
    if (theme === "light" || theme === "dark") root.setAttribute("data-theme", theme); else root.removeAttribute("data-theme");
    Array.prototype.forEach.call(document.querySelectorAll("[data-theme-choice]"), function (button) {
      button.setAttribute("aria-pressed", String(button.getAttribute("data-theme-choice") === (theme || "system")));
    });
  }
  var saved = null;
  try { saved = localStorage.getItem(KEY); } catch (e) { }
  apply(saved);
  document.addEventListener("DOMContentLoaded", function () {
    apply(root.getAttribute("data-theme"));
    document.addEventListener("click", function (e) {
      var button = e.target.closest && e.target.closest("[data-theme-choice]");
      if (!button) return;
      var choice = button.getAttribute("data-theme-choice");
      apply(choice);
      try { localStorage.setItem(KEY, choice); } catch (x) { }
    });
  });
})();
