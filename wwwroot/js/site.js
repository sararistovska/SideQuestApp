"use strict";

document.addEventListener("DOMContentLoaded", () => {
    initMobileMenu();
    initResponsiveNav();
    initQuestCardTilt();
    initSmoothScroll();
    initHeroStampMovement();
    initLeaderboardScrollToSearchedPlayer();
    initLikeButtons();
    initCopyGroupCode();
    initQuestCreateForm();
    initFriendRequests();
    initQuestFiltering();
    initCategoryDropdownFilter();
    initModals();
    initDarkMode();
    initCookieConsent();
    initNotifications();
    initTempMessages();
    initPendingReviews();
    initProfileImageUpload();
});

/* MOBILE MENU */
function initMobileMenu() {
    const button = document.querySelector(".sq-menu-toggle");
    const nav = document.querySelector(".sq-nav");
    if (!button || !nav) return;

    button.addEventListener("click", () => nav.classList.toggle("menu-open"));
}

/* RESPONSIVE NAV */
function initResponsiveNav() {
    const button = document.querySelector(".sq-mobile-menu-button");
    const nav = document.querySelector(".sq-mobile-nav");
    const overlay = document.querySelector(".sq-mobile-nav-overlay");
    if (!button || !nav || !overlay) return;

    const close = () => {
        button.classList.remove("is-open");
        nav.classList.remove("is-open");
        overlay.classList.remove("is-open");
        button.setAttribute("aria-expanded", "false");
        document.body.classList.remove("sq-mobile-menu-open");
    };

    button.addEventListener("click", () => {
        const open = nav.classList.toggle("is-open");
        button.classList.toggle("is-open", open);
        overlay.classList.toggle("is-open", open);
        button.setAttribute("aria-expanded", String(open));
        document.body.classList.toggle("sq-mobile-menu-open", open);
    });

    overlay.addEventListener("click", close);
    nav.querySelectorAll("a").forEach(link => link.addEventListener("click", close));
}

/* QUEST CARD TILT */
function initQuestCardTilt() {
    document.querySelectorAll(".sq-quest-card").forEach(card => {
        card.addEventListener("mousemove", e => {
            const r = card.getBoundingClientRect();
            const x = e.clientX - r.left - r.width / 2;
            const y = e.clientY - r.top - r.height / 2;

            card.style.transform =
                `translateY(-8px) rotateX(${(y / (r.height / 2)) * -1.5}deg) rotateY(${(x / (r.width / 2)) * 1.5}deg)`;
        });

        card.addEventListener("mouseleave", () => card.style.transform = "");
    });
}

/* SMOOTH SCROLL */
function initSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(link => {
        link.addEventListener("click", e => {
            const id = link.getAttribute("href");
            if (!id || id === "#") return;

            const target = document.querySelector(id);
            if (!target) return;

            e.preventDefault();
            target.scrollIntoView({ behavior: "smooth", block: "start" });
        });
    });
}

/* HERO STAMP */
function initHeroStampMovement() {
    const stamp = document.querySelector(".sq-hero-stamp");
    if (!stamp) return;

    window.addEventListener("scroll", () => {
        stamp.style.transform = `rotate(${10 + window.scrollY * 0.025}deg)`;
    });
}

/* LEADERBOARD */
function initLeaderboardScrollToSearchedPlayer() {
    const player = document.querySelector(".sq-search-target");
    if (player) {
        setTimeout(() => player.scrollIntoView({ behavior: "smooth", block: "center" }), 300);
    }
}

/* LIKES */
function initLikeButtons() {
    document.querySelectorAll(".sq-like-form").forEach(form => {
        const button = form.querySelector(".sq-like-button");
        const symbol = form.querySelector(".sq-like-symbol");
        const count = form.querySelector(".sq-like-count");
        const id = form.dataset.completionId;

        if (!button || !id) return;

        button.addEventListener("click", async () => {
            if (button.disabled) return;
            button.disabled = true;

            const liked = button.dataset.liked === "true";
            const token = form.querySelector('input[name="__RequestVerificationToken"]');

            if (!token) {
                button.disabled = false;
                return;
            }

            try {
                const response = await fetch(`/Feed/${liked ? "Unlike" : "Like"}?id=${id}`, {
                    method: "POST",
                    headers: {
                        "RequestVerificationToken": token.value,
                        "X-Requested-With": "XMLHttpRequest"
                    }
                });

                const data = await response.json();
                if (!response.ok || !data.success) throw new Error("Like request failed.");

                button.dataset.liked = String(data.liked);
                if (count) count.textContent = data.count;

                button.classList.toggle("sq-liked", data.liked);
                if (symbol) symbol.textContent = data.liked ? "♥" : "♡";

                button.classList.remove("sq-like-pop");
                void button.offsetWidth;
                button.classList.add("sq-like-pop");
            } catch (error) {
                console.error(error);
            } finally {
                button.disabled = false;
            }
        });
    });
}

/* COPY GROUP CODE */
function initCopyGroupCode() {
    document.addEventListener("click", async e => {
        const button = e.target.closest(".sq-copy-code");
        if (!button || !button.dataset.copyCode) return;

        const original = button.textContent;
        button.textContent = await copyToClipboard(button.dataset.copyCode) ? "Copied" : "Copy failed";

        setTimeout(() => button.textContent = original, 1600);
    });
}

async function copyToClipboard(text) {
    if (navigator.clipboard && window.isSecureContext) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch {}
    }

    const textarea = document.createElement("textarea");
    textarea.value = text;
    textarea.style.cssText = "position:fixed;left:-9999px;top:0";
    document.body.appendChild(textarea);
    textarea.focus();
    textarea.select();

    let copied = false;
    try {
        copied = document.execCommand("copy");
    } catch {}

    textarea.remove();
    return copied;
}

/* QUEST CREATE FORM */
function initQuestCreateForm() {
    const photo = document.getElementById("photo");
    const fileName = document.getElementById("sq-file-name");
    const fileHint = document.getElementById("sq-file-hint");

    if (photo && fileName && fileHint) {
        photo.addEventListener("change", () => {
            const file = photo.files?.[0];

            fileName.textContent = file?.name || "Choose a photo";
            fileHint.textContent = file
                ? `${(file.size / (1024 * 1024)).toFixed(2)} MB`
                : "JPG, JPEG, PNG or WEBP · max 10 MB";
        });
    }

    const caption = document.getElementById("caption");
    const count = document.getElementById("sq-caption-count");

    if (caption && count) {
        const update = () => count.textContent = caption.value.length;
        caption.addEventListener("input", update);
        update();
    }
}

/* FRIEND REQUESTS */
function initFriendRequests() {
    document.addEventListener("submit", e => {
        const form = e.target.closest("form");
        if (!form) return;

        const action = getFriendAction(form);
        if (!action && !form.classList.contains("sq-friend-ajax-form")) return;

        e.preventDefault();
        handleFriendForm(form);
    });
}

function getFriendAction(form) {
    if (form.dataset.action) return form.dataset.action;

    const url = (form.action || "").toLowerCase();
    if (url.includes("/friends/sendrequest")) return "send";
    if (url.includes("/friends/cancelrequest")) return "cancel";
    if (url.includes("/friends/accept")) return "accept";
    if (url.includes("/friends/reject")) return "reject";
    if (url.includes("/friends/remove")) return "remove";
    return "";
}

function getUsername(form) {
    return form.querySelector('input[name="username"]')?.value ||
        form.dataset.username || "";
}

function setFriendState(form, action) {
    const button = form.querySelector("button");
    if (!button) return;

    const send = action === "send";

    form.action = send ? "/Friends/SendRequest" : "/Friends/CancelRequest";
    form.dataset.action = action;

    button.disabled = false;
    button.textContent = send ? "Add friend" : "Cancel request";
    button.classList.toggle("sq-friend-action-primary", send);
    button.classList.toggle("sq-friend-action-secondary", !send);
}

function removeFriendElement(element) {
    if (!element) return;

    element.style.cssText = "transition:opacity .22s ease,transform .22s ease;opacity:0;transform:translateY(-8px)";
    setTimeout(() => element.remove(), 220);
}

async function handleFriendForm(form) {
    const action = getFriendAction(form);
    if (!action) return;

    const username = getUsername(form);
    if ((action === "send" || action === "cancel") && !username) {
        alert("Username is missing.");
        return;
    }

    const button = form.querySelector("button");
    const original = button?.textContent.trim();

    if (button) {
        button.disabled = true;
        button.textContent = "Loading...";
    }

    try {
        const data = new FormData(form);
        if (username && !data.has("username")) data.append("username", username);

        const response = await fetch(form.action, {
            method: "POST",
            body: data,
            headers: { "X-Requested-With": "XMLHttpRequest" },
            credentials: "same-origin"
        });

        const text = await response.text();
        let result;

        try {
            result = JSON.parse(text);
        } catch {
            throw new Error("Server did not return JSON.");
        }

        if (!response.ok || !result.success) {
            throw new Error(result.message || `Request failed. (${response.status})`);
        }

        if (action === "send" || action === "cancel") {
            setFriendState(form, action === "send" ? "cancel" : "send");
            return;
        }

        const element = form.closest(".sq-friend-person") || form.closest(".sq-friend-card");
        element ? removeFriendElement(element) : location.reload();
    } catch (error) {
        console.error(error);

        if (button) {
            button.disabled = false;
            button.textContent = original || "Try again";
        }

        alert(error.message || "Something went wrong. Please try again.");
    }
}

/* QUEST FILTERING */
function initQuestFiltering() {
    const search = document.getElementById("questSearch");
    const difficulty = document.querySelectorAll(".sq-filter");
    const category = document.querySelectorAll(".sq-category-filter");
    const cards = document.querySelectorAll(".sq-quest-card");
    const empty = document.getElementById("noQuestResults");

    if (!search || !cards.length) return;

    let activeDifficulty = "all";
    let activeCategory = "all";

    const filter = () => {
        const query = search.value.trim().toLowerCase();
        let visible = 0;

        cards.forEach(card => {
            const title = (card.dataset.title || "").toLowerCase();
            const cat = (card.dataset.category || "").toLowerCase();
            const diff = (card.dataset.difficulty || "").toLowerCase();

            const show =
                (title.includes(query) || cat.includes(query)) &&
                (activeDifficulty === "all" || diff === activeDifficulty.toLowerCase()) &&
                (activeCategory === "all" || cat === activeCategory.toLowerCase());

            card.style.display = show ? "" : "none";
            if (show) visible++;
        });

        if (empty) empty.style.display = visible ? "none" : "block";
    };

    difficulty.forEach(button => {
        button.addEventListener("click", () => {
            difficulty.forEach(b => b.classList.remove("active"));
            button.classList.add("active");
            activeDifficulty = button.dataset.filter || "all";
            filter();
        });
    });

    category.forEach(button => {
        button.addEventListener("click", () => {
            category.forEach(b => b.classList.remove("active"));
            button.classList.add("active");
            activeCategory = button.dataset.category || "all";
            filter();
        });
    });

    search.addEventListener("input", filter);
    filter();
}

/* CATEGORY DROPDOWN */
function initCategoryDropdownFilter() {
    const filter = document.getElementById("categoryFilter");
    if (!filter) return;

    filter.addEventListener("change", () => {
        const value = filter.value.toLowerCase();

        document.querySelectorAll(".sq-quest-card").forEach(card => {
            const category = (card.dataset.category || "").toLowerCase();
            card.style.display = value === "all" || category === value ? "" : "none";
        });
    });
}

/* MODALS */
function initModals() {
    const modals = document.querySelectorAll(".sq-quest-modal");
    const posts = document.querySelectorAll("[data-completion-modal]");

    if (!modals.length && !posts.length) return;

    const open = modal => {
        if (!modal) return;
        modal.classList.add("is-open");
        modal.setAttribute("aria-hidden", "false");
        document.body.classList.add("sq-modal-open");
    };

    const close = modal => {
        if (!modal) return;

        modal.classList.remove("is-open");
        modal.setAttribute("aria-hidden", "true");

        if (!document.querySelector(".sq-quest-modal.is-open")) {
            document.body.classList.remove("sq-modal-open");
        }
    };

    document.querySelectorAll("[data-quest-modal]").forEach(button => {
        button.addEventListener("click", e => {
            e.preventDefault();
            open(document.getElementById(`questModal-${button.dataset.questModal}`));
        });
    });

    posts.forEach(post => {
        post.addEventListener("click", e => {
            if (e.target.closest("[data-no-modal]")) return;

            const modal = document.getElementById("communityPostModal");
            if (!modal) return;

            const username = post.dataset.username || "User";
            const caption = post.dataset.caption || "";
            const avatar = post.querySelector(".sq-completion-avatar");
            const modalAvatar = document.getElementById("communityModalAvatar");

            const image = document.getElementById("communityModalImage");
            const name = document.getElementById("communityModalUsername");
            const date = document.getElementById("communityModalDate");
            const text = document.getElementById("communityModalCaption");
            const wrapper = document.getElementById("communityModalCaptionWrapper");
            const points = document.getElementById("communityModalPoints");

            if (image) {
                image.src = post.dataset.photo || "";
                image.alt = `Quest completion by ${username}`;
            }

            if (name) {
                name.textContent = username;
                name.href = post.dataset.profile || "#";
            }

            if (date) date.textContent = post.dataset.date || "";
            if (text) text.textContent = caption;
            if (wrapper) wrapper.style.display = caption.trim() ? "" : "none";
            if (points) points.textContent = `+${post.dataset.points || "0"} points`;

            if (modalAvatar) {
                modalAvatar.innerHTML = "";

                const avatarImage = avatar?.querySelector("img");
                if (avatarImage) {
                    modalAvatar.appendChild(avatarImage.cloneNode(true));
                } else {
                    modalAvatar.textContent =
                        avatar?.textContent.trim() ||
                        username.charAt(0).toUpperCase();
                }
            }

            open(modal);
        });
    });

    document.querySelectorAll(
        ".sq-quest-modal [data-modal-close], .sq-quest-modal [data-close-quest-modal]"
    ).forEach(button => {
        button.addEventListener("click", e => {
            e.preventDefault();
            close(button.closest(".sq-quest-modal"));
        });
    });

    document.getElementById("communityPostModal")?.addEventListener("click", e => {
        if (e.target.closest("[data-modal-close]")) {
            close(e.currentTarget);
        }
    });

    document.addEventListener("keydown", e => {
        if (e.key === "Escape") {
            close(document.querySelector(".sq-quest-modal.is-open"));
            const community = document.getElementById("communityPostModal");
            if (community?.classList.contains("is-open")) close(community);
        }
    });
}

/* DARK MODE */
function initDarkMode() {
    const toggle = document.getElementById("themeToggle");
    if (!toggle) return;

    const key = "sidequest-theme";

    const update = () => {
        toggle.classList.toggle("is-dark", document.body.classList.contains("sq-dark"));
    };

    if (localStorage.getItem(key) === "dark") {
        document.body.classList.add("sq-dark");
    }

    update();

    toggle.addEventListener("click", () => {
        const dark = document.body.classList.toggle("sq-dark");
        localStorage.setItem(key, dark ? "dark" : "light");
        update();
    });
}

/* COOKIE CONSENT */
function initCookieConsent() {
    const banner = document.getElementById("cookieBanner");
    const modal = document.getElementById("cookieModal");
    const analytics = document.getElementById("analyticsCookies");
    const name = "sidequest-cookie-consent";

    const getConsent = () => {
        const cookie = document.cookie
            .split(";")
            .map(c => c.trim())
            .find(c => c.startsWith(`${name}=`));

        if (!cookie) return null;

        try {
            return JSON.parse(decodeURIComponent(cookie.substring(name.length + 1)));
        } catch {
            return null;
        }
    };

    const hideBanner = () => {
        if (!banner) return;
        banner.classList.remove("is-visible");
        setTimeout(() => banner.hidden = true, 250);
    };

    const closeModal = () => {
        if (!modal) return;
        modal.classList.remove("is-visible");
        setTimeout(() => modal.hidden = true, 200);
        document.body.classList.remove("sq-cookie-modal-open");
    };

    const save = value => {
        const expires = new Date();
        expires.setFullYear(expires.getFullYear() + 1);

        document.cookie =
            `${name}=${encodeURIComponent(JSON.stringify({
    essential: true,
    analytics: value,
    timestamp: new Date().toISOString()
}))}; expires=${expires.toUTCString()}; path=/; SameSite=Lax`;

hideBanner();
closeModal();
};

const openModal = () => {
    if (!modal) return;

    analytics && (analytics.checked = getConsent()?.analytics === true);
    modal.hidden = false;

    requestAnimationFrame(() => modal.classList.add("is-visible"));
    document.body.classList.add("sq-cookie-modal-open");
};

document.getElementById("cookieAccept")?.addEventListener("click", () => save(true));
document.getElementById("cookieNecessary")?.addEventListener("click", () => save(false));
document.getElementById("cookieManage")?.addEventListener("click", openModal);
document.getElementById("cookieSave")?.addEventListener("click", () => save(analytics?.checked === true));
document.getElementById("cookieAcceptModal")?.addEventListener("click", () => save(true));
document.getElementById("cookieModalClose")?.addEventListener("click", closeModal);
document.getElementById("cookieModalBackdrop")?.addEventListener("click", closeModal);
document.getElementById("cookieSettings")?.addEventListener("click", openModal);

if (!getConsent() && banner) {
    banner.hidden = false;
    requestAnimationFrame(() => banner.classList.add("is-visible"));
}
}

/* NOTIFICATIONS */
function initNotifications() {
    const toggle = document.getElementById("notificationToggle");
    const dropdown = document.getElementById("notificationDropdown");
    if (!toggle || !dropdown) return;

    toggle.addEventListener("click", e => {
        e.stopPropagation();
        const open = dropdown.classList.toggle("is-open");
        toggle.setAttribute("aria-expanded", String(open));
    });

    dropdown.addEventListener("click", e => e.stopPropagation());

    document.addEventListener("click", () => {
        dropdown.classList.remove("is-open");
        toggle.setAttribute("aria-expanded", "false");
    });
}

/* TEMPORARY MESSAGES */
function initTempMessages() {
    document.querySelectorAll(".sq-temp-message").forEach(message => {
        setTimeout(() => {
            message.classList.add("sq-temp-message-hide");
            setTimeout(() => message.remove(), 300);
        }, 4000);
    });
}

/* PENDING REVIEWS */
function initPendingReviews() {
    const list = document.getElementById("reviewList");
    const count = document.getElementById("pendingReviewCount");
    if (!list) return;

    list.addEventListener("submit", async e => {
        const form = e.target.closest(".sq-review-form");
        if (!form) return;

        e.preventDefault();

        const button = form.querySelector("button[type='submit']");
        const card = form.closest(".sq-review-card");
        if (!button || !card) return;

        const original = button.textContent;
        button.disabled = true;
        button.textContent = "Processing...";

        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: new FormData(form),
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            const result = await response.json();

            if (!response.ok || !result.success) {
                throw new Error(result.message || "Something went wrong. Please try again.");
            }

            card.style.cssText =
                "transition:opacity 180ms ease,transform 180ms ease;opacity:0;transform:translateY(-8px)";

            setTimeout(() => {
                card.remove();

                const remaining = list.querySelectorAll(".sq-review-card").length;
                if (count) count.textContent = remaining;

                if (!remaining) {
                    list.remove();

                    const content = document.getElementById("reviewContent");
                    if (content) {
                        content.insertAdjacentHTML("beforeend", `
                            <div class="sq-review-empty">
                                <div class="sq-review-empty-mark">0</div>
                                <div>
                                    <div class="sq-section-eyebrow">ALL CLEAR</div>
                                    <h2>No pending completions.</h2>
                                    <p>Everything has been reviewed for now.</p>
                                </div>
                            </div>
                        `);
                    }
                }
            }, 190);
        } catch (error) {
            console.error(error);
            button.disabled = false;
            button.textContent = original;
            alert(error.message || "Something went wrong. Please try again.");
        }
    });
}

/* PROFILE IMAGE UPLOAD */
function initProfileImageUpload() {
    const input = document.getElementById("profileImage");
    const name = document.getElementById("sq-profile-file-name");
    const hint = document.getElementById("sq-profile-file-hint");

    if (!input || !name || !hint) return;

    input.addEventListener("change", () => {
        const file = input.files?.[0];

        name.textContent = file?.name || "Choose a new profile picture";
        hint.textContent = file
            ? "New image selected"
            : "JPG, JPEG, PNG or WEBP · max 10 MB";
    });
}

