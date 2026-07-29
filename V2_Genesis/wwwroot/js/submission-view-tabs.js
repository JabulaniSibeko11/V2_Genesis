document.addEventListener("DOMContentLoaded", function () {
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
