(() => {
  "use strict";

  const apiBase = (window.SIGEBI_API_BASE || "").replace(/\/$/, "");
  const state = {
    token: sessionStorage.getItem("sigebi.token"),
    user: JSON.parse(sessionStorage.getItem("sigebi.user") || "null"),
    roles: JSON.parse(sessionStorage.getItem("sigebi.roles") || "[]"),
    books: [],
    summary: null,
    requests: [],
    notifications: []
  };
  if (!state.roles.length) {
    state.roles = [sessionStorage.getItem("sigebi.role") || "Usuario"];
  }
  const primaryRole = () =>
    ["Administrador", "Bibliotecario", "Auditor", "Usuario"]
      .find(role => state.roles.includes(role)) || state.roles[0] || "Usuario";
  const hasRole = (...roles) => roles.some(role => state.roles.includes(role));

  const $ = (selector, root = document) => root.querySelector(selector);
  const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
  const content = $("#content");
  const titles = {
    inicio: "Inicio",
    catalogo: "Explorar catálogo",
    solicitudes: "Mis solicitudes",
    prestamos: "Mis préstamos",
    multas: "Mis multas",
    notificaciones: "Notificaciones",
    cuenta: "Mi cuenta",
    reportes: "Reportes"
  };

  const escapeHtml = value => String(value ?? "")
    .replaceAll("&", "&amp;").replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;").replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
  const date = value => value
    ? new Intl.DateTimeFormat("es-DO", { dateStyle: "medium" }).format(new Date(value))
    : "—";
  const money = value => new Intl.NumberFormat("es-DO", {
    style: "currency", currency: "DOP", maximumFractionDigits: 0
  }).format(value || 0);

  async function api(path, options = {}) {
    const headers = { Accept: "application/json", ...(options.headers || {}) };
    if (state.token) headers.Authorization = `Bearer ${state.token}`;
    if (options.body && !(options.body instanceof FormData)) headers["Content-Type"] = "application/json";
    const response = await fetch(`${apiBase}/${path.replace(/^\//, "")}`, { ...options, headers });
    if (response.status === 401) {
      logout();
      throw new Error("Tu sesión venció. Inicia sesión nuevamente.");
    }
    if (!response.ok) {
      const type = response.headers.get("content-type") || "";
      const payload = type.includes("json") ? await response.json() : await response.text();
      throw new Error(payload.detail || payload.title || payload || "No fue posible completar la operación.");
    }
    if (response.status === 204) return null;
    return response.json();
  }

  function toast(message, error = false) {
    const element = $("#toast");
    element.textContent = message;
    element.className = `toast show${error ? " error" : ""}`;
    clearTimeout(toast.timer);
    toast.timer = setTimeout(() => element.className = "toast", 3300);
  }

  function setLoading() {
    content.innerHTML = $("#loading-template").innerHTML;
  }

  function empty(title, text) {
    return `<div class="empty"><strong>${escapeHtml(title)}</strong>${escapeHtml(text)}</div>`;
  }

  function showApp() {
    $("#login-view").hidden = true;
    $("#app-view").hidden = false;
    const fullName = `${state.user?.nombre || ""} ${state.user?.apellido || ""}`.trim() || "Usuario";
    $("#user-name").textContent = fullName;
    $("#user-role").textContent = state.roles.join(", ");
    $("#user-initials").textContent = fullName.split(/\s+/).slice(0, 2).map(x => x[0]).join("").toUpperCase();
    const canReport = hasRole("Administrador", "Auditor");
    $(".report-link").hidden = !canReport;
  }

  async function login(event) {
    event.preventDefault();
    const button = $("#login-form button[type=submit]");
    const error = $("#login-error");
    button.disabled = true;
    error.hidden = true;
    try {
      const response = await api("Auth/login", {
        method: "POST",
        body: JSON.stringify({
          email: $("#email").value.trim(),
          password: $("#password").value
        })
      });
      state.token = response.token;
      state.user = response.usuario;
      state.roles = Array.isArray(response.roles) && response.roles.length
        ? response.roles
        : [response.rol || "Usuario"];
      sessionStorage.setItem("sigebi.token", state.token);
      sessionStorage.setItem("sigebi.user", JSON.stringify(state.user));
      sessionStorage.setItem("sigebi.roles", JSON.stringify(state.roles));
      sessionStorage.setItem("sigebi.role", primaryRole());
      showApp();
      await navigate("inicio");
    } catch (exception) {
      error.textContent = exception.message;
      error.hidden = false;
    } finally {
      button.disabled = false;
    }
  }

  function logout() {
    sessionStorage.removeItem("sigebi.token");
    sessionStorage.removeItem("sigebi.user");
    sessionStorage.removeItem("sigebi.role");
    sessionStorage.removeItem("sigebi.roles");
    state.token = null;
    state.user = null;
    $("#app-view").hidden = true;
    $("#login-view").hidden = false;
  }

  async function refreshSummary() {
    const [summary, requests] = await Promise.all([
      api("Usuarios/me/resumen"),
      api("SolicitudesPrestamo/mias")
    ]);
    state.summary = summary;
    state.requests = requests;
    state.user = state.summary.usuario;
    state.notifications = state.summary.notificaciones || [];
    sessionStorage.setItem("sigebi.user", JSON.stringify(state.user));
    const unread = state.notifications.filter(item => !item.leida).length;
    const badge = $("#notification-badge");
    badge.textContent = unread;
    badge.hidden = unread === 0;
    return state.summary;
  }

  async function renderHome() {
    setLoading();
    const summary = await refreshSummary();
    const active = (summary.prestamos || []).filter(x => ["Activo", "Vencido"].includes(x.estado));
    const pendingFines = (summary.multas || []).filter(x => x.estado === "Pendiente");
    const unread = (summary.notificaciones || []).filter(x => !x.leida);
    content.innerHTML = `
      <div class="hero">
        <div>
          <span class="eyebrow">Hola, ${escapeHtml(summary.usuario.nombre)}</span>
          <h3>Hay un libro esperando por ti.</h3>
          <p>Consulta las novedades de la biblioteca, solicita un ejemplar y revisa tus fechas de devolución.</p>
          <button class="card-button" data-go="catalogo">Explorar catálogo →</button>
        </div>
        <div class="hero-stat"><strong>${active.length}</strong><small>préstamos activos</small></div>
      </div>
      <div class="stats-grid">
        <article class="stat-card"><span>Solicitudes registradas</span><strong>${state.requests.length}</strong></article>
        <article class="stat-card"><span>Multas pendientes</span><strong>${pendingFines.length}</strong></article>
        <article class="stat-card"><span>Notificaciones nuevas</span><strong>${unread.length}</strong></article>
      </div>
      <div class="section-head"><div><h3>Próximas devoluciones</h3><p>Mantente al día y evita penalizaciones.</p></div></div>
      <div class="list">
        ${active.length ? active.slice(0, 4).map(loan => `
          <article class="list-card">
            <div><h4>Préstamo #${loan.id}</h4><p>Libro ${loan.libroId} · Ejemplar ${loan.ejemplarId}</p></div>
            <div><span class="pill ${escapeHtml(loan.estado)}">${escapeHtml(loan.estado)}</span><small>Vence ${date(loan.fechaEsperadaDevolucion)}</small></div>
          </article>`).join("") : empty("Todo al día", "No tienes préstamos activos en este momento.")}
      </div>`;
  }

  async function loadBooks(filters = {}) {
    const query = new URLSearchParams();
    Object.entries(filters).forEach(([key, value]) => {
      if (value !== "" && value != null) query.set(key, value);
    });
    state.books = await api(`Libros/buscar?${query}`);
    return state.books;
  }

  function bookCards(books) {
    if (!books.length) return empty("Sin resultados", "Prueba con otros términos o filtros.");
    return `<div class="books-grid">${books.map(book => `
      <article class="book-card">
        <div class="book-cover">${escapeHtml((book.titulo || "L").slice(0, 1).toUpperCase())}</div>
        <h4>${escapeHtml(book.titulo)}</h4>
        <p>${escapeHtml(book.autor)} · ${escapeHtml(book.genero || "Sin categoría")}</p>
        <div class="book-meta">
          <span class="pill ${book.disponible ? "" : "unavailable"}">${book.disponible ? `${book.cantidadDisponible} disponible${book.cantidadDisponible === 1 ? "" : "s"}` : "No disponible"}</span>
          <button class="card-button request-book" data-book-id="${book.id}" ${book.disponible ? "" : "disabled"}>Solicitar</button>
        </div>
      </article>`).join("")}</div>`;
  }

  async function renderCatalog() {
    setLoading();
    const books = await loadBooks();
    content.innerHTML = `
      <form id="catalog-filter" class="toolbar">
        <input name="termino" aria-label="Buscar por título, autor o ISBN" placeholder="Título, autor o ISBN…" />
        <input name="genero" aria-label="Filtrar por género" placeholder="Género" />
        <input name="editorial" aria-label="Filtrar por editorial" placeholder="Editorial" />
        <select name="disponible" aria-label="Filtrar disponibilidad">
          <option value="">Toda disponibilidad</option><option value="true">Disponible</option><option value="false">No disponible</option>
        </select>
      </form>
      <div id="catalog-results">${bookCards(books)}</div>`;
    $("#catalog-filter").addEventListener("input", debounce(async event => {
      const form = new FormData(event.currentTarget);
      const results = await loadBooks(Object.fromEntries(form));
      $("#catalog-results").innerHTML = bookCards(results);
    }, 280));
  }

  async function requestBook(bookId, button) {
    button.disabled = true;
    try {
      await api("SolicitudesPrestamo", {
        method: "POST",
        body: JSON.stringify({ libroId: Number(bookId), usuarioId: state.user.id })
      });
      toast("Solicitud registrada. Te avisaremos cuando sea revisada.");
    } catch (exception) {
      toast(exception.message, true);
    } finally {
      button.disabled = false;
    }
  }

  async function renderRequests() {
    setLoading();
    state.requests = await api("SolicitudesPrestamo/mias");
    content.innerHTML = `
      <div class="section-head"><div><h3>Historial de solicitudes</h3><p>Consulta el avance de cada solicitud.</p></div><button class="card-button" data-go="catalogo">Nueva solicitud</button></div>
      <div class="list">${state.requests.length ? state.requests.map(item => `
        <article class="list-card">
          <div><h4>Solicitud #${item.id}</h4><p>Libro ${item.libroId} · Solicitada ${date(item.fechaSolicitud)}</p></div>
          <div>
            <span class="pill ${escapeHtml(item.estado)}">${escapeHtml(item.estado)}</span>
            ${item.estado === "Pendiente"
              ? `<button class="card-button cancel-request" data-request-id="${item.id}">Cancelar</button>`
              : ""}
          </div>
        </article>`).join("") : empty("Todavía no has solicitado libros", "Explora el catálogo para comenzar.")}</div>`;
  }

  async function cancelRequest(id, button) {
    button.disabled = true;
    try {
      await api(`SolicitudesPrestamo/${id}`, { method: "DELETE" });
      toast("Solicitud cancelada correctamente.");
      await renderRequests();
    } catch (exception) {
      toast(exception.message, true);
      button.disabled = false;
    }
  }

  async function renderLoans() {
    setLoading();
    const summary = await refreshSummary();
    const loans = summary.prestamos || [];
    content.innerHTML = `
      <div class="section-head"><div><h3>Tus préstamos</h3><p>Activos, vencidos y devueltos en un solo historial.</p></div></div>
      <div class="list">${loans.length ? loans.map(item => `
        <article class="list-card">
          <div><h4>Préstamo #${item.id}</h4><p>Libro ${item.libroId} · Desde ${date(item.fechaPrestamo)} · Límite ${date(item.fechaEsperadaDevolucion)}</p></div>
          <div><span class="pill ${escapeHtml(item.estado)}">${escapeHtml(item.estado)}</span><small>${item.fechaRealDevolucion ? `Devuelto ${date(item.fechaRealDevolucion)}` : "Pendiente de devolución"}</small></div>
        </article>`).join("") : empty("Sin préstamos", "Cuando se apruebe una solicitud aparecerá aquí.")}</div>`;
  }

  async function renderFines() {
    setLoading();
    const summary = await refreshSummary();
    const fines = summary.multas || [];
    content.innerHTML = `
      <div class="section-head"><div><h3>Multas y penalizaciones</h3><p>Revisa el estado y motivo de cada registro.</p></div></div>
      <div class="list">${fines.length ? fines.map(item => `
        <article class="list-card">
          <div><h4>${escapeHtml(item.tipo)} · ${money(item.monto)}</h4><p>${escapeHtml(item.motivo)} · Generada ${date(item.fechaGeneracion)}</p></div>
          <span class="pill ${escapeHtml(item.estado)}">${escapeHtml(item.estado)}</span>
        </article>`).join("") : empty("No tienes multas", "Tu cuenta se encuentra libre de penalizaciones.")}</div>`;
  }

  async function renderNotifications() {
    setLoading();
    state.notifications = await api("Notificaciones/mias");
    content.innerHTML = `
      <div class="section-head"><div><h3>Bandeja de notificaciones</h3><p>Avisos sobre solicitudes, préstamos y vencimientos.</p></div></div>
      <div class="list">${state.notifications.length ? state.notifications.map(item => `
        <article class="list-card ${item.leida ? "" : "unread"}">
          <div><h4>${escapeHtml(item.tipoEvento)}</h4><p>${escapeHtml(item.mensaje)}</p><small>${date(item.fechaEnvio)}</small></div>
          ${item.leida ? '<span class="pill">Leída</span>' : `<button class="card-button mark-read" data-notification-id="${item.id}">Marcar leída</button>`}
        </article>`).join("") : empty("Sin notificaciones", "Los avisos importantes aparecerán aquí.")}</div>`;
  }

  async function markRead(id, button) {
    button.disabled = true;
    try {
      await api(`Notificaciones/${id}/leer`, { method: "PUT" });
      await renderNotifications();
      const unread = state.notifications.filter(x => !x.leida).length;
      $("#notification-badge").textContent = unread;
      $("#notification-badge").hidden = unread === 0;
    } catch (exception) {
      toast(exception.message, true);
      button.disabled = false;
    }
  }

  async function renderAccount() {
    const summary = state.summary || await refreshSummary();
    const user = summary.usuario;
    content.innerHTML = `
      <div class="account-grid">
        <article class="panel">
          <span class="eyebrow">Perfil bibliotecario</span>
          <h3>${escapeHtml(user.nombre)} ${escapeHtml(user.apellido)}</h3>
          <div class="detail-grid">
            <div><small>Correo</small>${escapeHtml(user.email)}</div>
            <div><small>Cédula</small>${escapeHtml(user.cedula)}</div>
            <div><small>Teléfono</small>${escapeHtml(user.telefono || "No registrado")}</div>
            <div><small>Estado</small><span class="pill">${escapeHtml(user.estado)}</span></div>
          </div>
        </article>
        <article class="panel">
          <span class="eyebrow">Seguridad</span>
          <h3>Cambiar contraseña</h3>
          <form id="password-form" class="inline-form">
            <label>Contraseña actual<input type="password" name="passwordActual" required /></label>
            <label>Nueva contraseña<input type="password" name="passwordNueva" minlength="8" required /></label>
            <button class="primary-button" type="submit"><span>Actualizar contraseña</span><span>→</span></button>
          </form>
        </article>
      </div>`;
  }

  async function changePassword(event) {
    event.preventDefault();
    const button = $("button[type=submit]", event.currentTarget);
    button.disabled = true;
    try {
      await api("Usuarios/me/password", {
        method: "PUT",
        body: JSON.stringify(Object.fromEntries(new FormData(event.currentTarget)))
      });
      event.currentTarget.reset();
      toast("Contraseña actualizada correctamente.");
    } catch (exception) {
      toast(exception.message, true);
    } finally {
      button.disabled = false;
    }
  }

  async function renderReports() {
    if (!hasRole("Administrador", "Auditor")) return navigate("inicio");
    const now = new Date();
    const from = new Date(now.getFullYear(), now.getMonth() - 1, now.getDate()).toISOString();
    const to = now.toISOString();
    setLoading();
    const query = `desde=${encodeURIComponent(from)}&hasta=${encodeURIComponent(to)}`;
    const [catalog, loans, fines] = await Promise.all([
      api(`Reportes/catalogo?${query}`),
      api(`Reportes/prestamos-fecha?${query}`),
      api(`Reportes/multas?${query}`)
    ]);
    content.innerHTML = `
      <div class="section-head"><div><h3>Resumen de los últimos 30 días</h3><p>Indicadores operativos para supervisión.</p></div></div>
      <div class="report-grid">
        <article class="panel"><span class="eyebrow">Catálogo</span><div class="report-number">${catalog.disponibilidadPromedioPorcentaje}%</div><p>Disponibilidad promedio</p></article>
        <article class="panel"><span class="eyebrow">Préstamos</span><div class="report-number">${loans.totalPrestamos}</div><p>${loans.tasaDevolucionPuntualPorcentaje}% devuelto a tiempo</p></article>
        <article class="panel"><span class="eyebrow">Multas</span><div class="report-number">${fines.generadas}</div><p>${money(fines.montoTotal)} generados</p></article>
      </div>
      <div class="section-head"><div><h3>Recursos más solicitados</h3></div></div>
      <div class="list">${catalog.recursosMasSolicitados.length ? catalog.recursosMasSolicitados.map(item => `
        <article class="list-card"><div><h4>${escapeHtml(item.titulo)}</h4><p>${escapeHtml(item.genero)}</p></div><strong>${item.solicitudes} préstamos</strong></article>`).join("") : empty("Sin actividad", "No hay préstamos registrados en este período.")}</div>`;
  }

  const renderers = {
    inicio: renderHome, catalogo: renderCatalog, solicitudes: renderRequests,
    prestamos: renderLoans, multas: renderFines, notificaciones: renderNotifications,
    cuenta: renderAccount, reportes: renderReports
  };

  async function navigate(view) {
    if (!renderers[view]) view = "inicio";
    $$(".nav-item[data-view]").forEach(item => item.classList.toggle("active", item.dataset.view === view));
    $("#page-title").textContent = titles[view];
    $(".sidebar").classList.remove("open");
    try {
      await renderers[view]();
    } catch (exception) {
      content.innerHTML = empty("No pudimos cargar esta sección", exception.message);
      toast(exception.message, true);
    }
  }

  function debounce(callback, wait) {
    let timer;
    return (...args) => {
      clearTimeout(timer);
      timer = setTimeout(() => callback(...args), wait);
    };
  }

  document.addEventListener("click", event => {
    const nav = event.target.closest("[data-view], [data-go]");
    if (nav) navigate(nav.dataset.view || nav.dataset.go);
    const request = event.target.closest(".request-book");
    if (request) requestBook(request.dataset.bookId, request);
    const mark = event.target.closest(".mark-read");
    if (mark) markRead(mark.dataset.notificationId, mark);
    const cancellation = event.target.closest(".cancel-request");
    if (cancellation) cancelRequest(cancellation.dataset.requestId, cancellation);
  });
  document.addEventListener("submit", event => {
    if (event.target.id === "password-form") changePassword(event);
  });
  $("#login-form").addEventListener("submit", login);
  $("#logout-button").addEventListener("click", logout);
  $("#menu-button").addEventListener("click", () => $(".sidebar").classList.toggle("open"));
  $("#toggle-password").addEventListener("click", event => {
    const input = $("#password");
    input.type = input.type === "password" ? "text" : "password";
    event.currentTarget.textContent = input.type === "password" ? "Mostrar" : "Ocultar";
  });

  if (state.token && state.user) {
    showApp();
    navigate("inicio");
  }
})();
