// Renders a static, schema-driven form/preview into `root` from a UI schema JSON document
// produced by the application builder. This is a PREVIEW ONLY renderer: it never executes any
// code contained in the schema and never calls a live backend — actions just show a toast.
function renderSchema(root, schema) {
    "use strict";

    while (root.firstChild) {
        root.removeChild(root.firstChild);
    }

    if (!schema || !schema.pages || schema.pages.length === 0) {
        root.textContent = "No preview available for this application.";
        return;
    }

    var page = schema.pages[0];

    var title = document.createElement("h5");
    title.textContent = page.name || schema.title || "Preview";
    root.appendChild(title);

    var form = document.createElement("form");
    form.className = "row g-3";
    form.addEventListener("submit", function (e) {
        e.preventDefault();
    });

    (page.fields || []).forEach(function (field) {
        var col = document.createElement("div");
        col.className = "col-12";

        var label = document.createElement("label");
        label.className = "form-label";
        label.textContent = field.label || field.name || "";
        col.appendChild(label);

        var control;

        if (field.type === "select") {
            control = document.createElement("select");
            control.className = "form-select";
            (field.options || []).forEach(function (opt) {
                var option = document.createElement("option");
                option.value = opt;
                option.textContent = opt;
                control.appendChild(option);
            });
        } else if (field.type === "textarea") {
            control = document.createElement("textarea");
            control.className = "form-control";
            control.setAttribute("rows", "3");
        } else {
            control = document.createElement("input");
            control.setAttribute("type", "text");
            control.className = "form-control";
        }

        if (field.name) {
            control.setAttribute("name", field.name);
        }

        col.appendChild(control);
        form.appendChild(col);
    });

    root.appendChild(form);

    if (page.actions && page.actions.length > 0) {
        var actionsRow = document.createElement("div");
        actionsRow.className = "mt-3";

        page.actions.forEach(function (action) {
            var btn = document.createElement("button");
            btn.type = "button";
            btn.className = "btn btn-outline-primary me-2";
            btn.textContent = action;
            btn.addEventListener("click", function () {
                showPreviewToast(action + " — sample action, not wired to a live backend in preview.");
            });
            actionsRow.appendChild(btn);
        });

        root.appendChild(actionsRow);
    }
}

function showPreviewToast(message) {
    "use strict";

    var container = document.getElementById("preview-toast-container");
    if (!container) {
        container = document.createElement("div");
        container.id = "preview-toast-container";
        container.className = "toast-container position-fixed bottom-0 end-0 p-3";
        document.body.appendChild(container);
    }

    var toast = document.createElement("div");
    toast.className = "toast align-items-center text-bg-secondary border-0";
    toast.setAttribute("role", "alert");

    var flexWrap = document.createElement("div");
    flexWrap.className = "d-flex";

    var body = document.createElement("div");
    body.className = "toast-body";
    body.textContent = message;

    var closeBtn = document.createElement("button");
    closeBtn.type = "button";
    closeBtn.className = "btn-close btn-close-white me-2 m-auto";
    closeBtn.setAttribute("data-bs-dismiss", "toast");

    flexWrap.appendChild(body);
    flexWrap.appendChild(closeBtn);
    toast.appendChild(flexWrap);
    container.appendChild(toast);

    if (window.bootstrap && window.bootstrap.Toast) {
        var bsToast = new window.bootstrap.Toast(toast, { delay: 4000 });
        bsToast.show();
        toast.addEventListener("hidden.bs.toast", function () {
            toast.remove();
        });
    } else {
        window.setTimeout(function () { toast.remove(); }, 4000);
    }
}
