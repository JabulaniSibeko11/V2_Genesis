(function () {
    'use strict';

    /*
     * SECTION 6 CATEGORY RESET
     *
     * Business rule:
     * - Category is optional unless the client actually wants to change it.
     * - If the client selects the same category that is already reflected
     *   on the Valuation Roll / MVD, clear the dropdown immediately.
     * - Do NOT force the client to choose another category.
     * - The client may leave Category blank and change only Extent,
     *   Market Value, Address, Owner, Property Description, etc.
     *
     * This works for:
     * - Objection
     * - Appeal
     * - Section 78 Query
     * - Section 78 Review
     * - Multipurpose versions of the above
     */

    const categoryPairs = {
        NewCat: 'cat',
        NewCat1: 'cat1',
        NewCat2: 'cat2',
        NewCat3: 'cat3'
    };

    function normaliseText(value) {
        return String(value ?? '')
            .trim()
            .toLowerCase()
            .replace(/\s+/g, ' ');
    }

    function normaliseCategory(value) {
        const v = normaliseText(value);

        const aliases = {
            'residential': 'residential property',
            'residential property': 'residential property',

            'business': 'business and commercial',
            'commercial': 'business and commercial',
            'business and commercial': 'business and commercial',

            'agric': 'agricultural',
            'agriculture': 'agricultural',
            'agricultural': 'agricultural',

            'multiple purposes': 'multipurpose',
            'multipurpose': 'multipurpose',
            'multipurpose*': 'multipurpose',
            'multi purpose': 'multipurpose',
            'multi-purpose': 'multipurpose',

            'vacant': 'vacant land',
            'vacant land': 'vacant land'
        };

        return aliases[v] || v;
    }

    function clearCategoryErrors(select) {
        if (!select) return;

        select.style.border = '';
        select.style.backgroundColor = '';

        const sameValueError =
            document.getElementById(
                select.id + '_same_error');

        sameValueError?.remove();

        /*
         * Section 78 creates validation messages dynamically.
         * Remove only messages attached to this category control.
         */
        const parent = select.parentElement;

        if (parent) {
            parent
                .querySelectorAll(
                    '.field-error, .validation-error, .same-value-error')
                .forEach(x => x.remove());
        }
    }

    function showCategoryResetMessage() {
        const message =
            'The selected category is already reflected on the Valuation Roll. ' +
            'The Category field has been cleared. Leave it blank if you do not want to change the category.';

        /*
         * Use the existing Genesis toaster where the page provides it.
         * No modal/error is required because this is not a validation failure.
         */
        if (typeof window.showToaster === 'function') {
            window.showToaster(
                'Category unchanged',
                message,
                3000);

            return;
        }

        /*
         * Some legacy pages expose showToaster as a normal global function
         * rather than a window property.
         */
        if (typeof showToaster === 'function') {
            showToaster(
                'Category unchanged',
                message,
                3000);
        }
    }

    function resetSameCategory(select, oldInput) {
        const selectedCategory =
            normaliseCategory(select.value);

        const rollCategory =
            normaliseCategory(oldInput.value);

        if (
            !selectedCategory ||
            !rollCategory ||
            selectedCategory !== rollCategory
        ) {
            return false;
        }

        /*
         * Reset to the blank "--Select Category--" option.
         * This makes Category an unchanged field rather than forcing
         * the client to select a different category.
         */
        select.value = '';

        clearCategoryErrors(select);
        showCategoryResetMessage();

        return true;
    }

    /*
     * Capture phase is intentional.
     *
     * The existing legacy JS files already contain same-category handlers
     * that show an error/modal and tell the client to select a different
     * category. We intercept the category change before those handlers run.
     */
    document.addEventListener(
        'change',
        function (event) {
            const select = event.target;

            if (!(select instanceof HTMLSelectElement)) {
                return;
            }

            const oldCategoryId =
                categoryPairs[select.id];

            if (!oldCategoryId) {
                return;
            }

            const oldInput =
                document.getElementById(
                    oldCategoryId);

            if (!oldInput) {
                return;
            }

            const wasReset =
                resetSameCategory(
                    select,
                    oldInput);

            if (!wasReset) {
                return;
            }

            /*
             * Stop the legacy "same value" handlers from treating the
             * cleared category as an error and forcing another selection.
             */
            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();
        },
        true
    );
})();
