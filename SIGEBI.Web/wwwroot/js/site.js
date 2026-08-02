document.addEventListener("DOMContentLoaded", () => {
  const sidebar = document.getElementById("sidebar");
  const backdrop = document.getElementById("sidebarBackdrop");
  const closeSidebar = () => {
    sidebar?.classList.remove("open");
    document.body.classList.remove("sidebar-open");
  };
  document.getElementById("menuButton")?.addEventListener("click", () => {
    const isOpen = sidebar?.classList.toggle("open") ?? false;
    document.body.classList.toggle("sidebar-open", isOpen);
  });
  backdrop?.addEventListener("click", closeSidebar);
  sidebar?.querySelectorAll("a").forEach(link =>
    link.addEventListener("click", closeSidebar));
  document.addEventListener("keydown", event => {
    if (event.key === "Escape") closeSidebar();
  });

  document.querySelector("[data-password-toggle]")?.addEventListener("click", event => {
    const button = event.currentTarget;
    const input = button.parentElement?.querySelector("input");
    if (!input) return;
    const visible = input.type === "text";
    input.type = visible ? "password" : "text";
    button.textContent = visible ? "Mostrar" : "Ocultar";
  });

  setTimeout(() => {
    document.querySelectorAll(".toast.show").forEach(toast => toast.classList.remove("show"));
  }, 4500);

  const confirmationDialog = document.getElementById("confirmationDialog");
  const confirmationMessage = document.getElementById("confirmationMessage");
  const confirmationAccept = document.getElementById("confirmationAccept");
  const confirmationCancel = document.getElementById("confirmationCancel");
  let pendingConfirmationForm = null;

  document.querySelectorAll("form[data-confirm]").forEach(form => {
    form.addEventListener("submit", event => {
      if (form.dataset.confirmed === "true") {
        delete form.dataset.confirmed;
        return;
      }
      event.preventDefault();
      pendingConfirmationForm = form;
      if (confirmationMessage)
        confirmationMessage.textContent = form.dataset.confirm ?? "Confirma esta operación.";
      confirmationDialog?.showModal();
    });
  });

  confirmationAccept?.addEventListener("click", () => {
    if (!pendingConfirmationForm) return;
    const form = pendingConfirmationForm;
    pendingConfirmationForm = null;
    confirmationDialog?.close();
    form.dataset.confirmed = "true";
    form.requestSubmit();
  });
  confirmationCancel?.addEventListener("click", () => {
    pendingConfirmationForm = null;
    confirmationDialog?.close();
  });

  const revealItems = document.querySelectorAll(
    ".hero, .stat-card, .book-card, .list-card, .panel");
  revealItems.forEach((item, index) => {
    item.classList.add("reveal-item");
    item.style.setProperty("--reveal-delay", `${Math.min(index * 55, 330)}ms`);
  });
  requestAnimationFrame(() => {
    revealItems.forEach(item => item.classList.add("is-visible"));
  });
});
