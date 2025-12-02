(() => {
    // === Diccionario de traducciones ===
    const t = {
        en: {
            "brand.name": "LAWDGE IA",
            "nav.home": "HOME",
            "nav.about": "ABOUT",
            "nav.services": "SERVICES",
            "nav.contact": "CONTACT",

            "hero.title": "Less is More",
            "hero.subtitle": "Minimalist Design Studio",
            "hero.cta": "EXPLORE",

            "stats.satisfaction": "Client Satisfaction",
            "stats.projects": "Projects Completed",
            "stats.years": "Years Experience",
            "stats.possibilities": "Possibilities",

            "about.title": "Philosophy",
            "about.lead": "Analyze long contracts in seconds with AI: find and summarize key clauses without reading the entire document.",
            "about.c1.h": "Clarity First",
            "about.c1.p": "Strip away complexity to reveal truth",
            "about.c2.h": "Space to Breathe",
            "about.c2.p": "Let design elements have room to speak",
            "about.c3.h": "Intentional Choices",
            "about.c3.p": "Every decision has meaning and purpose",

            "process.title": "Our Process",
            "process.discover.h": "Discover",
            "process.discover.p": "Understanding your essence",
            "process.reduce.h": "Reduce",
            "process.reduce.p": "Removing the unnecessary",
            "process.refine.h": "Refine",
            "process.refine.p": "Perfecting every detail",
            "process.deliver.h": "Deliver",
            "process.deliver.p": "Bringing vision to life",

            "values.title": "Core Values",
            "values.minimalism": "Minimalism",
            "values.innovation": "Innovation",
            "values.precision": "Precision",
            "values.balance": "Balance",

            "services.title": "What We Do",
            "services.brand.h": "Brand Identity",
            "services.brand.p": "Creating distinctive visual languages that communicate your essence through simplicity. We develop comprehensive brand systems that stand the test of time.",
            "services.brand.cta": "View Work",
            "services.s1": "Strategy First",
            "services.s2": "Digital Native",
            "services.s3": "Vision Focused",
            "services.web.h": "Web Design",
            "services.web.p": "Crafting digital experiences that prioritize clarity and usability. Every pixel serves a purpose in our pursuit of functional beauty.",
            "services.web.cta": "Explore",
            "services.art.h": "Art Direction",
            "services.art.p": "Guiding visual narratives with restraint and intention. We believe the most powerful statements are often the quietest ones.",
            "services.art.cta": "Discover",

            "contact.title": "Connect",
            "contact.email": "Email",
            "contact.phone": "Phone",
            "contact.location": "Location",
            "contact.send": "Send",

            "form.name": "Name",
            "form.email": "Email",
            "form.message": "Message",

            "footer.copy": "© 2025 Minimal Studio — Designed by"
        },

        es: {
            "brand.name": "LAWDGE IA",
            "nav.home": "INICIO",
            "nav.about": "NOSOTROS",
            "nav.services": "SERVICIOS",
            "nav.contact": "CONTACTO",

            "hero.title": "Menos es Más",
            "hero.subtitle": "Estudio de Diseño Minimalista",
            "hero.cta": "EXPLORAR",

            "stats.satisfaction": "Satisfacción del Cliente",
            "stats.projects": "Proyectos Completados",
            "stats.years": "Años de Experiencia",
            "stats.possibilities": "Posibilidades",

            "about.title": "Filosofía",
            "about.lead": "Analiza contratos extensos en segundos con IA: encuentra y resume cláusulas clave sin leer todo el documento.",
            "about.c1.h": "Claridad Primero",
            "about.c1.p": "Quita la complejidad para revelar la verdad",
            "about.c2.h": "Espacio para Respirar",
            "about.c2.p": "Deja que los elementos hablen con holgura",
            "about.c3.h": "Elecciones Intencionales",
            "about.c3.p": "Cada decisión tiene sentido y propósito",

            "process.title": "Nuestro Proceso",
            "process.discover.h": "Descubrir",
            "process.discover.p": "Comprender tu esencia",
            "process.reduce.h": "Reducir",
            "process.reduce.p": "Eliminar lo innecesario",
            "process.refine.h": "Refinar",
            "process.refine.p": "Perfeccionar cada detalle",
            "process.deliver.h": "Entregar",
            "process.deliver.p": "Hacer realidad la visión",

            "values.title": "Valores Fundamentales",
            "values.minimalism": "Minimalismo",
            "values.innovation": "Innovación",
            "values.precision": "Precisión",
            "values.balance": "Balance",

            "services.title": "Qué Hacemos",
            "services.brand.h": "Identidad de Marca",
            "services.brand.p": "Creamos lenguajes visuales distintivos que comunican tu esencia con simplicidad. Desarrollamos sistemas de marca que perduran en el tiempo.",
            "services.brand.cta": "Ver trabajos",
            "services.s1": "Estrategia Primero",
            "services.s2": "Nativos Digitales",
            "services.s3": "Visión Enfocada",
            "services.web.h": "Diseño Web",
            "services.web.p": "Experiencias digitales que priorizan claridad y usabilidad. Cada píxel cumple un propósito hacia una belleza funcional.",
            "services.web.cta": "Explorar",
            "services.art.h": "Dirección de Arte",
            "services.art.p": "Narrativas visuales con mesura e intención. A veces, los mensajes más poderosos son los más silenciosos.",
            "services.art.cta": "Descubrir",

            "contact.title": "Conectar",
            "contact.email": "Correo",
            "contact.phone": "Teléfono",
            "contact.location": "Ubicación",
            "contact.send": "Enviar",

            "form.name": "Nombre",
            "form.email": "Correo",
            "form.message": "Mensaje",

            "footer.copy": "© 2025 Minimal Studio — Diseñado por"
        }
    };

    //  Aplicar traducciones 
    function applyI18n(lang) {
        const dict = t[lang] || t.en;


        document.querySelectorAll("[data-i18n]").forEach(el => {
            const key = el.getAttribute("data-i18n");
            if (dict[key] != null) el.textContent = dict[key];
        });

        document.querySelectorAll("[data-i18n-label]").forEach(el => {
            const key = el.getAttribute("data-i18n-label");
            if (dict[key] != null) {
                const label = el.parentElement?.querySelector("label");
                if (label) label.textContent = dict[key];
            }
        });

        document.documentElement.setAttribute("lang", lang);

        document.querySelectorAll(".lang-btn").forEach(btn => {
            btn.setAttribute("aria-pressed", String(btn.dataset.lang === lang));
        });
        const hint = document.getElementById("langHint");
        if (hint) hint.textContent = lang === "es" ? "(es)" : "(en)";
    }

    const saved = localStorage.getItem("lang");
    const initial = saved || document.documentElement.getAttribute("lang") || "en";
    applyI18n(initial);

    // === 4) Eventos de click en EN/ES ===
    document.addEventListener("click", (e) => {
        const btn = e.target.closest(".lang-btn");
        if (!btn) return;
        const lang = btn.dataset.lang;
        localStorage.setItem("lang", lang);
        applyI18n(lang);
    });
})();