let currentConvId = 0;
let isTyping = false;
let selectedDocumentId = null;
let selectedDocumentName = null;

document.addEventListener("DOMContentLoaded", () => {
    initTheme();
    loadUserInfo();
    loadConversations();
    loadDocuments();
    setupEventListeners();
    loadInitialConversation();
});

function initTheme() {
    const savedTheme = localStorage.getItem("lawdeia-theme") || "dark";
    document.documentElement.setAttribute("data-theme", savedTheme);
    updateThemeIcon(savedTheme);
}

function updateThemeIcon(theme) {
    const icon = document.querySelector('#theme-toggle i');
    if (icon) {
        icon.className = theme === "dark" ? "fas fa-sun" : "fas fa-moon";
    }
}

function toggleTheme() {
    const current = document.documentElement.getAttribute("data-theme");
    const newTheme = current === "dark" ? "light" : "dark";
    document.documentElement.setAttribute("data-theme", newTheme);
    localStorage.setItem("lawdeia-theme", newTheme);
    updateThemeIcon(newTheme);
}

async function loadUserInfo() {
    try {
        // En una implementación real, esto vendría del backend
        const userAvatar = document.getElementById('user-avatar');
        const userName = document.getElementById('user-name');
        const userEmail = document.getElementById('user-email');

        // Por ahora, usamos valores por defecto
        userAvatar.textContent = 'U';
        userName.textContent = 'Usuario';
        userEmail.textContent = 'usuario@ejemplo.com';
    } catch (error) {
        console.error('Error loading user info:', error);
    }
}

function setupEventListeners() {
    const input = document.getElementById("message-input");
    if (input) {
        input.addEventListener("keypress", e => {
            if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });

        input.addEventListener("input", function () {
            this.style.height = 'auto';
            this.style.height = (this.scrollHeight) + 'px';
        });
    }

    document.getElementById("send-btn")?.addEventListener("click", sendMessage);
    document.getElementById("new-chat-btn")?.addEventListener("click", newChat);
    document.getElementById("theme-toggle")?.addEventListener("click", toggleTheme);
    document.getElementById("clear-doc-btn")?.addEventListener("click", clearSelectedDocument);
    document.getElementById("upload-doc-btn")?.addEventListener("click", () => document.getElementById("file-input").click());

    // Upload logic
    const uploadBtn = document.getElementById("upload-btn");
    const fileInput = document.getElementById("file-input");
    if (uploadBtn && fileInput) {
        uploadBtn.addEventListener("click", () => fileInput.click());
        fileInput.addEventListener("change", async function () {
            if (this.files.length > 0) {
                await uploadFile(this.files[0]);
                this.value = '';
            }
        });
    }
}

function newChat() {
    currentConvId = 0;
    selectedDocumentId = null;
    selectedDocumentName = null;
    updateSelectedDocumentUI();
    showEmptyChat();
    history.replaceState(null, "", "/Chat");
}

async function uploadFile(file) {
    if (!validateFile(file)) return;

    appendMessage({ content: `📎 <em>Subiendo ${file.name}...</em>`, senderType: "System" });

    const formData = new FormData();
    formData.append("file", file);

    try {
        const res = await fetch("/Chat/UploadDocument", {
            method: "POST",
            body: formData
        });
        const data = await res.json();

        if (data.success) {
            appendMessage({ content: `✅ Documento "${data.document.title}" procesado correctamente.`, senderType: "System" });
            await loadDocuments();
        } else {
            appendMessage({ content: `❌ Error: ${data.error}`, senderType: "System" });
        }
    } catch (e) {
        appendMessage({ content: "❌ Error de conexión al subir archivo.", senderType: "System" });
    }
}

function validateFile(file) {
    const validTypes = ['.txt', '.md', '.pdf', '.jpg', '.jpeg', '.png'];
    const maxSize = 50 * 1024 * 1024; // 50MB

    const fileExt = '.' + file.name.split('.').pop().toLowerCase();
    if (!validTypes.includes(fileExt)) {
        alert('Tipo de archivo no válido. Formatos aceptados: TXT, MD, PDF, JPG, PNG');
        return false;
    }

    if (file.size > maxSize) {
        alert('El archivo es demasiado grande. Tamaño máximo: 50MB');
        return false;
    }

    return true;
}

function loadInitialConversation() {
    const params = new URLSearchParams(location.search);
    const convId = params.get("conversationId");
    if (convId && !isNaN(convId)) {
        loadConversation(parseInt(convId));
    } else {
        showEmptyChat();
    }
}

function showEmptyChat() {
    currentConvId = 0;
    const messagesDiv = document.getElementById("messages");
    if (messagesDiv) {
        messagesDiv.innerHTML = `
            <div class="empty-chat">
                <h3>Bienvenido a LawdeIA</h3>
                <p>${selectedDocumentId ? `Documento seleccionado: ${selectedDocumentName}` : 'Selecciona un documento y comienza tu consulta legal.'}</p>
            </div>`;
    }
    const title = document.getElementById("chat-title");
    if (title) title.textContent = "Nueva consulta";
}

async function loadConversations() {
    try {
        const res = await fetch("/Chat/GetConversations");
        const data = res.ok ? await res.json() : [];
        const sidebar = document.getElementById("conversations-sidebar");
        if (!sidebar) return;

        if (data.length === 0) {
            sidebar.innerHTML = '<div style="padding:15px; text-align:center; opacity:0.6">Sin historial</div>';
            return;
        }

        sidebar.innerHTML = data.map(c => `
            <div class="conv-item ${c.id === currentConvId ? 'active' : ''}" onclick="loadConversation(${c.id})">
                <div class="conv-title">${escapeHtml(c.title || 'Chat')}</div>
                <button class="delete-conv" onclick="event.stopPropagation(); deleteConversation(${c.id})" title="Eliminar conversación">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
        `).join('');
    } catch (e) {
        console.error('Error loading conversations:', e);
    }
}

async function loadDocuments() {
    try {
        const res = await fetch("/Chat/GetUserDocuments");
        const data = res.ok ? await res.json() : [];
        const documentsList = document.getElementById("documents-list");
        if (!documentsList) return;

        if (data.length === 0) {
            documentsList.innerHTML = '<div style="padding:10px; text-align:center; opacity:0.6; font-size:0.8rem;">No hay documentos</div>';
            return;
        }

        documentsList.innerHTML = data.map(doc => `
            <div class="document-item ${doc.id === selectedDocumentId ? 'selected' : ''}" 
                 onclick="selectDocument(${doc.id}, '${escapeHtml(doc.title)}')">
                <i class="fas fa-file doc-icon"></i>
                <div class="doc-title">${escapeHtml(doc.title)}</div>
                <div class="doc-actions">
                    <button class="btn-doc-action btn-delete" onclick="event.stopPropagation(); deleteDocument(${doc.id})" title="Eliminar documento">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>
        `).join('');
    } catch (e) {
        console.error('Error loading documents:', e);
    }
}

function selectDocument(docId, docName) {
    selectedDocumentId = docId;
    selectedDocumentName = docName;
    updateSelectedDocumentUI();
    loadDocuments(); // Refresh to update selection styles

    // Si hay una conversación activa, mostrar mensaje del cambio
    if (currentConvId > 0) {
        appendMessage({
            content: `📄 Documento seleccionado: <strong>${docName}</strong>. Las próximas respuestas se basarán en este documento.`,
            senderType: "System"
        });
    }
}

function updateSelectedDocumentUI() {
    const selectedDocElement = document.getElementById("selected-document");
    const selectedDocNameElement = document.getElementById("selected-doc-name");

    if (selectedDocumentId) {
        selectedDocElement.style.display = 'flex';
        selectedDocNameElement.textContent = selectedDocumentName;
    } else {
        selectedDocElement.style.display = 'none';
    }

    // Actualizar el estado vacío del chat
    const emptyChat = document.querySelector('.empty-chat');
    if (emptyChat) {
        emptyChat.querySelector('p').textContent =
            selectedDocumentId ?
                `Documento seleccionado: ${selectedDocumentName}` :
                'Selecciona un documento y comienza tu consulta legal.';
    }
}

function clearSelectedDocument() {
    selectedDocumentId = null;
    selectedDocumentName = null;
    updateSelectedDocumentUI();
    loadDocuments(); // Refresh to update selection styles

    if (currentConvId > 0) {
        appendMessage({
            content: "📄 Documento deseleccionado. Las respuestas usarán toda la información disponible.",
            senderType: "System"
        });
    }
}

async function loadConversation(id) {
    if (!id) return showEmptyChat();

    try {
        const res = await fetch(`/Chat/LoadConversation?conversationId=${id}`);
        const data = await res.json();

        if (!data.success) {
            showEmptyChat();
            return;
        }

        currentConvId = id;
        const div = document.getElementById("messages");
        div.innerHTML = "";

        if (data.messages && data.messages.length > 0) {
            data.messages.forEach(m => appendMessage(m));
        } else {
            showEmptyChat();
        }

        const title = document.getElementById("chat-title");
        if (title) title.textContent = data.title;

        history.replaceState(null, "", "?conversationId=" + id);
        loadConversations(); // Refresh sidebar
    } catch (e) {
        console.error('Error loading conversation:', e);
        showEmptyChat();
    }
}

async function sendMessage() {
    if (isTyping) return;

    const input = document.getElementById("message-input");
    const val = input.value.trim();
    if (!val) return;

    // Clear empty state if it exists
    const emptyChat = document.querySelector('.empty-chat');
    if (emptyChat) {
        emptyChat.remove();
    }

    appendMessage({
        content: val,
        senderType: "User",
        timestamp: new Date().toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' })
    });

    input.value = "";
    input.style.height = 'auto';

    showTyping(true);

    try {
        const res = await fetch("/Chat/SendMessage", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                message: val,
                conversationId: currentConvId || 0,
                selectedDocumentId: selectedDocumentId
            })
        });

        const data = await res.json();
        showTyping(false);

        if (data.success) {
            currentConvId = data.conversationId;
            appendMessage({
                content: data.aiMessage.content,
                senderType: "AI",
                timestamp: new Date().toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' })
            });
            loadConversations();
        } else {
            appendMessage({
                content: "❌ Error: " + (data.error || "Error desconocido"),
                senderType: "System"
            });
        }
    } catch (e) {
        showTyping(false);
        appendMessage({
            content: "❌ Error de conexión con el servidor",
            senderType: "System"
        });
    }
}

async function deleteConversation(id) {
    if (!confirm("¿Estás seguro de que quieres eliminar esta conversación?")) return;

    try {
        await fetch("/Chat/DeleteConversation", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ conversationId: id })
        });

        await loadConversations();
        if (currentConvId === id) {
            newChat();
        }
    } catch (e) {
        console.error('Error deleting conversation:', e);
        alert('Error al eliminar la conversación');
    }
}

async function deleteDocument(docId) {
    if (!confirm("¿Estás seguro de que quieres eliminar este documento? Esta acción no se puede deshacer.")) return;

    try {
        const res = await fetch("/Chat/DeleteDocument", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ documentId: docId })
        });

        const data = await res.json();
        if (data.success) {
            if (selectedDocumentId === docId) {
                clearSelectedDocument();
            }
            await loadDocuments();
            appendMessage({
                content: "✅ Documento eliminado correctamente.",
                senderType: "System"
            });
        } else {
            alert('Error al eliminar documento: ' + data.error);
        }
    } catch (e) {
        console.error('Error deleting document:', e);
        alert('Error al eliminar el documento');
    }
}

function showTyping(show) {
    isTyping = show;
    const input = document.getElementById("message-input");
    const sendBtn = document.getElementById("send-btn");

    if (input) input.disabled = show;
    if (sendBtn) sendBtn.disabled = show;

    if (show) {
        const typingEl = document.createElement("div");
        typingEl.className = "message ai";
        typingEl.id = "typing-indicator";
        typingEl.innerHTML = `
            <div class="bubble">
                <div class="typing-indicator">
                    <span>LawdeIA está escribiendo</span>
                    <div class="typing-dots">
                        <div class="typing-dot"></div>
                        <div class="typing-dot"></div>
                        <div class="typing-dot"></div>
                    </div>
                </div>
            </div>
        `;
        document.getElementById("messages").appendChild(typingEl);
        scrollToBottom();
    } else {
        const typingEl = document.getElementById("typing-indicator");
        if (typingEl) typingEl.remove();
        if (input) input.focus();
    }
}

function appendMessage(m) {
    const messagesDiv = document.getElementById("messages");
    if (!messagesDiv) return;

    // Remove empty state if it exists
    const emptyChat = messagesDiv.querySelector('.empty-chat');
    if (emptyChat) emptyChat.remove();

    // Contenedor del mensaje
    const messageEl = document.createElement("div");
    messageEl.className = `message ${m.senderType.toLowerCase()}`;

    // Contenedor interno (necesario para centrar)
    const inner = document.createElement("div");
    inner.className = "msg-inner";

    // Burbuja
    const bubble = document.createElement("div");
    bubble.className = "bubble";
    bubble.innerHTML = formatMessageContent(m.content);

    inner.appendChild(bubble);
    messageEl.appendChild(inner);
    messagesDiv.appendChild(messageEl);

    scrollToBottom();
}


function formatMessageContent(content) {
    if (!content) return '';

    // Convertir saltos de línea
    let formatted = content.replace(/\n/g, '<br>');

    // Formatear código inline (entre backticks)
    formatted = formatted.replace(/`([^`]+)`/g, '<code class="inline-code">$1</code>');

    // Formatear bloques de código (entre triple backticks)
    formatted = formatted.replace(/```([^`]+)```/g, '<pre class="code-block">$1</pre>');

    return formatted;
}

function scrollToBottom() {
    const messagesDiv = document.getElementById("messages");
    if (messagesDiv) {
        messagesDiv.scrollTop = messagesDiv.scrollHeight;
    }
}

function escapeHtml(unsafe) {
    if (!unsafe) return '';
    return unsafe
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}