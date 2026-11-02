(() => {
    const section = document.querySelector("[data-dress-rail-section]");
    if (!section) return;

    const track = section.querySelector("[data-dress-rail-track]");
    const items = Array.from(section.querySelectorAll("[data-dress-rail-item]"));
    const feature = section.querySelector("[data-dress-rail-feature]");
    const featureImage = section.querySelector("[data-dress-rail-feature-image]");
    const featureName = section.querySelector("[data-dress-rail-feature-name]");
    const featurePrice = section.querySelector("[data-dress-rail-feature-price]");
    const featureLink = section.querySelector("[data-dress-rail-feature-link]");
    const dataNode = section.querySelector("[data-dress-rail-data]");
    if (!track || !items.length || !feature || !dataNode) return;

    let products;
    try { products = JSON.parse(dataNode.textContent); } catch { return; }

    let activeIndex = -1;
    let pointerDown = false;
    let dragged = false;
    let startX = 0;
    let startScroll = 0;
    let hoverFrame = 0;
    let featureTimer;

    function preview(index) {
        const product = products[index];
        if (!product) return;
        activeIndex = index;
        items.forEach((item, i) => {
            const active = i === index;
            item.classList.toggle("is-selected", active);
            item.setAttribute("aria-pressed", active ? "true" : "false");
        });

        feature.hidden = false;
        feature.classList.remove("is-entering");
        void feature.offsetWidth;
        feature.classList.add("is-entering");

        if (featureName) featureName.textContent = product.name || "";
        if (featurePrice) featurePrice.textContent = product.price || "";
        if (featureLink) featureLink.href = product.url || "#";

        if (featureImage) {
            featureImage.style.opacity = "0";
            window.clearTimeout(featureTimer);
            featureTimer = window.setTimeout(() => {
                featureImage.src = product.image || "";
                featureImage.alt = product.name || "";
                featureImage.onload = () => { featureImage.style.opacity = "1"; };
                if (!product.image || featureImage.complete) featureImage.style.opacity = "1";
            }, 90);
        }
    }

    function clearPreview() {
        activeIndex = -1;
        items.forEach(item => {
            item.classList.remove("is-selected");
            item.setAttribute("aria-pressed", "false");
        });
        feature.hidden = true;
    }

    items.forEach((item, index) => {
        item.addEventListener("pointerenter", event => {
            if (event.pointerType === "mouse" || event.pointerType === "pen") preview(index);
        });
        item.addEventListener("focus", () => preview(index));
        item.addEventListener("click", () => preview(index));
    });

    section.addEventListener("pointerleave", event => {
        if (event.pointerType === "mouse" || event.pointerType === "pen") clearPreview();
    });
    section.addEventListener("focusout", event => {
        if (!section.contains(event.relatedTarget)) clearPreview();
    });

    // Move the rail gently as the pointer approaches either edge.
    track.addEventListener("pointermove", event => {
        if (event.pointerType !== "mouse" || pointerDown || track.scrollWidth <= track.clientWidth) return;
        const bounds = track.getBoundingClientRect();
        const ratio = Math.max(0, Math.min(1, (event.clientX - bounds.left) / bounds.width));
        const target = ratio * (track.scrollWidth - track.clientWidth);
        if (hoverFrame) cancelAnimationFrame(hoverFrame);
        hoverFrame = requestAnimationFrame(() => {
            track.scrollLeft += (target - track.scrollLeft) * 0.12;
        });
    });

    // Mouse drag and native touch swipe.
    track.addEventListener("pointerdown", event => {
        if (event.pointerType === "touch" || event.button !== 0) return;
        pointerDown = true;
        dragged = false;
        startX = event.clientX;
        startScroll = track.scrollLeft;
    });
    track.addEventListener("pointermove", event => {
        if (!pointerDown) return;
        const delta = event.clientX - startX;
        if (Math.abs(delta) > 5) {
            dragged = true;
            track.classList.add("is-dragging");
            track.scrollLeft = startScroll - delta;
        }
    });
    const stopDrag = () => {
        pointerDown = false;
        track.classList.remove("is-dragging");
    };
    track.addEventListener("pointerup", stopDrag);
    track.addEventListener("pointercancel", stopDrag);
    track.addEventListener("mouseleave", stopDrag);
    track.addEventListener("click", event => {
        if (!dragged) return;
        event.preventDefault();
        event.stopPropagation();
        dragged = false;
    }, true);

    track.addEventListener("keydown", event => {
        if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
        event.preventDefault();
        const current = activeIndex < 0 ? 0 : activeIndex;
        const next = Math.max(0, Math.min(items.length - 1, current + (event.key === "ArrowRight" ? 1 : -1)));
        items[next].focus();
        items[next].scrollIntoView({ behavior: "smooth", block: "nearest", inline: "center" });
    });
})();
