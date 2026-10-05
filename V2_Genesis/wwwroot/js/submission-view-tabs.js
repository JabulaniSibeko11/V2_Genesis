document.addEventListener("DOMContentLoaded", function () {
    // Empty read-only values: show "Not provided" (styled in Submission-View.css).
    document
        .querySelectorAll(".municipal-readonly-input, .municipal-readonly-textarea")
        .forEach(function (box) {
            if (box.children.length === 0 && box.textContent.trim() === "") {
                box.textContent = "Not provided";
                box.classList.add("is-empty");
            }
        });

    document
        .querySelectorAll("[data-submitted-form-tabs]")
        .forEach(function (container) {
            const tabs = Array.from(
                container.querySelectorAll("[data-section-key]")
            );

            const panels = Array.from(
                container.querySelectorAll("[data-section-panel]")
            );

            function activate(key, updateUrl) {
                tabs.forEach(function (tab) {
                    const active = tab.dataset.sectionKey === key;

                    tab.classList.toggle("active", active);
                    tab.setAttribute(
                        "aria-selected",
                        active ? "true" : "false"
                    );
                });

                panels.forEach(function (panel) {
                    const active =
                        panel.dataset.sectionPanel === key;

                    panel.classList.toggle("active", active);
                    panel.hidden = !active;
                });

                if (updateUrl) {
                    const url = new URL(window.location.href);
                    url.searchParams.set("section", key);
                    window.history.replaceState(
                        {},
                        "",
                        url.toString()
                    );
                }
            }

            tabs.forEach(function (tab) {
                tab.addEventListener("click", function () {
                    activate(tab.dataset.sectionKey, true);

                    // On phones the tabs take up the screen: bring the
                    // chosen section into view.
                    if (window.matchMedia("(max-width: 900px)").matches) {
                        const panel = container.querySelector(
                            '[data-section-panel="' + tab.dataset.sectionKey + '"]');
                        if (panel) {
                            const nav = document.querySelector("#clientNav, .cl-navbar");
                            const offset = nav ? nav.getBoundingClientRect().height + 12 : 12;
                            const top = panel.getBoundingClientRect().top + window.pageYOffset - offset;
                            window.scrollTo({ top: Math.max(top, 0), behavior: "smooth" });
                        }
                    }
                });
            });

            const url = new URL(window.location.href);
            const requested = url.searchParams.get("section");
            const requestedTab = tabs.find(
                tab => tab.dataset.sectionKey === requested
            );

            activate(
                requestedTab
                    ? requestedTab.dataset.sectionKey
                    : tabs[0]?.dataset.sectionKey,
                false
            );
        });
});
