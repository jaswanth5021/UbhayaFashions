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
    let suppressClick = false;
    let previewTimer = 0;
    let edgeFrame = 0;
    let pointerX = null;

    function showPreview(index) {
        const product = products[index];
        if (!product) return;
        activeIndex = index;

        items.forEach((item, i) => {
            const selected = i === index;
            item.classList.toggle("is-selected", selected);
            item.setAttribute("aria-pressed", selected ? "true" : "false");
        });

        feature.hidden = false;
        feature.classList.remove("is-entering");
        void feature.offsetWidth;
        feature.classList.add("is-entering");

        if (featureName) featureName.textContent = product.name || "";
        if (featurePrice) featurePrice.textContent = product.price || "";
        if (featureLink) featureLink.href = product.url || "#";

        if (featureImage && product.image) {
            window.clearTimeout(previewTimer);
            featureImage.style.opacity = "0";
            previewTimer = window.setTimeout(() => {
                featureImage.src = product.image;
                featureImage.alt = product.name || "";
                featureImage.onload = () => { featureImage.style.opacity = "1"; };
                if (featureImage.complete) featureImage.style.opacity = "1";
            }, 80);
        }
    }

    function hidePreview() {
        activeIndex = -1;
        items.forEach(item => {
            item.classList.remove("is-selected");
            item.setAttribute("aria-pressed", "false");
        });
        feature.hidden = true;
    }

    items.forEach((item, index) => {
        item.addEventListener("pointerenter", event => {
            if (event.pointerType === "mouse" || event.pointerType === "pen") showPreview(index);
        });
        item.addEventListener("focus", () => showPreview(index));
        item.addEventListener("click", () => {
            if (suppressClick) return;
            showPreview(index);
        });
    });

    // Keep the selected look visible while the pointer moves from the garment to its details.
    section.addEventListener("pointerleave", event => {
        if (event.pointerType === "mouse" || event.pointerType === "pen") hidePreview();
    });
    section.addEventListener("focusout", event => {
        if (!section.contains(event.relatedTarget)) hidePreview();
    });

    // Cursor position gently guides the rail left or right, like sliding hangers along a rack.
    function edgeScroll() {
        if (pointerX === null || pointerDown || track.scrollWidth <= track.clientWidth) {
            edgeFrame = 0;
            return;
        }
        const bounds = track.getBoundingClientRect();
        const localX = Math.max(0, Math.min(bounds.width, pointerX - bounds.left));
        const normalized = (localX / bounds.width - 0.5) * 2;
        const threshold = 0.18;
        let velocity = 0;
        if (normalized < -threshold) {
            velocity = -Math.pow((-normalized - threshold) / (1 - threshold), 2) * 13;
        } else if (normalized > threshold) {
            velocity = Math.pow((normalized - threshold) / (1 - threshold), 2) * 13;
        }
        if (Math.abs(velocity) > 0.15) track.scrollLeft += velocity;
        edgeFrame = requestAnimationFrame(edgeScroll);
    }

    track.addEventListener("pointermove", event => {
        if (event.pointerType !== "mouse" || pointerDown) return;
        pointerX = event.clientX;
        if (!edgeFrame) edgeFrame = requestAnimationFrame(edgeScroll);
    });
    track.addEventListener("pointerleave", () => {
        pointerX = null;
        if (edgeFrame) cancelAnimationFrame(edgeFrame);
        edgeFrame = 0;
    });

    // Desktop drag; touch devices retain their native horizontal swipe behaviour.
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
        if (!pointerDown) return;
        pointerDown = false;
        track.classList.remove("is-dragging");
        if (dragged) {
            suppressClick = true;
            window.setTimeout(() => { suppressClick = false; }, 0);
        }
    };
    track.addEventListener("pointerup", stopDrag);
    track.addEventListener("pointercancel", stopDrag);
    track.addEventListener("mouseleave", stopDrag);
    track.addEventListener("click", event => {
        if (!suppressClick) return;
        event.preventDefault();
        event.stopPropagation();
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
