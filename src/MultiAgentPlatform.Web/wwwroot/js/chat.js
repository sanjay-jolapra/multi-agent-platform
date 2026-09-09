(function () {
    "use strict";

    var root = document.querySelector("[data-conversation-id]");
    if (!root) {
        return;
    }

    var conversationId = root.getAttribute("data-conversation-id");
    var messagesEl = document.getElementById("messages");
    var input = document.getElementById("chatInput");
    var sendBtn = document.getElementById("chatSend");
    var previewRoot = document.getElementById("preview-root");

    function appendMessage(sender, content) {
        var wrapper = document.createElement("div");
        wrapper.className = "chat-message " + (sender === "user" ? "chat-message-user" : "chat-message-assistant");

        var senderEl = document.createElement("div");
        senderEl.className = "chat-message-sender";
        senderEl.textContent = sender === "user" ? "You" : "Assistant";

        var contentEl = document.createElement("div");
        contentEl.className = "chat-message-content";
        contentEl.textContent = content;

        wrapper.appendChild(senderEl);
        wrapper.appendChild(contentEl);
        messagesEl.appendChild(wrapper);
        messagesEl.scrollTop = messagesEl.scrollHeight;
    }

    (window.__chatHistory || []).forEach(function (m) {
        appendMessage(m.sender, m.content);
    });

    var connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/chat")
        .withAutomaticReconnect()
        .build();

    connection.on("ReceiveMessage", function (msg) {
        appendMessage(msg.sender, msg.content);
    });

    connection.on("ApplicationReady", function (app) {
        appendMessage("assistant", "Application \"" + app.name + "\" has been generated. Loading preview...");
        loadPreview(app.applicationId);
    });

    function loadPreview(applicationId) {
        fetch("/api/v1/applications/" + applicationId + "/ui-schema")
            .then(function (r) {
                if (!r.ok) {
                    throw new Error("Failed to load UI schema.");
                }
                return r.json();
            })
            .then(function (schema) {
                if (typeof renderSchema === "function") {
                    renderSchema(previewRoot, schema);
                }
            })
            .catch(function () {
                previewRoot.textContent = "Unable to load the preview for this application.";
            });
    }

    function send() {
        var text = input.value.trim();
        if (!text) {
            return;
        }

        appendMessage("user", text);
        input.value = "";

        connection.invoke("SendMessage", conversationId, text).catch(function (err) {
            appendMessage("assistant", "Failed to send message: " + err);
        });
    }

    sendBtn.addEventListener("click", send);
    input.addEventListener("keydown", function (e) {
        if (e.key === "Enter") {
            e.preventDefault();
            send();
        }
    });

    connection.start().catch(function (err) {
        appendMessage("assistant", "Unable to connect to chat: " + err);
    });
})();
