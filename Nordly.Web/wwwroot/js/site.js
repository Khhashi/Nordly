(() => {
	document.querySelectorAll('.action-toast').forEach(toast => {
		const dismiss = () => toast.remove();
		toast.querySelector('.toast-close')?.addEventListener('click', dismiss);
		window.setTimeout(dismiss, 5000);
	});

	document.querySelectorAll('.detail-stepper').forEach(stepper => {
		const input = stepper.querySelector('input');
		const buttons = [...stepper.querySelectorAll('[data-step]')];
		const min = Number(input.min) || 1;
		const max = Number(input.max) || 10;
		const sync = () => {
			const value = Math.min(max, Math.max(min, Number.parseInt(input.value, 10) || min));
			input.value = value.toString();
			buttons.forEach(button => button.disabled = Number(button.dataset.step) < 0 ? value <= min : value >= max);
		};
		buttons.forEach(button => button.addEventListener('click', () => {
			input.value = ((Number.parseInt(input.value, 10) || min) + Number(button.dataset.step)).toString();
			sync();
		}));
		input.addEventListener('change', sync);
		sync();
	});

	const selectGalleryImage = thumbnail => {
		const gallery = thumbnail.closest('[data-product-gallery]');
		const mainImage = gallery?.querySelector('[data-gallery-main]');
		if (!gallery || !mainImage) return;

		mainImage.src = thumbnail.dataset.galleryImage;
		mainImage.alt = thumbnail.dataset.galleryAlt || mainImage.alt;
		gallery.querySelectorAll('[data-gallery-thumbnail]').forEach(button => {
			button.setAttribute('aria-pressed', button === thumbnail ? 'true' : 'false');
		});
	};

	document.addEventListener('click', event => {
		const thumbnail = event.target.closest('[data-gallery-thumbnail]');
		if (thumbnail) selectGalleryImage(thumbnail);
	});

	document.addEventListener('keydown', event => {
		const thumbnail = event.target.closest('[data-gallery-thumbnail]');
		if (!thumbnail || !['ArrowLeft', 'ArrowRight'].includes(event.key)) return;

		const gallery = thumbnail.closest('[data-product-gallery]');
		if (!gallery) return;
		const thumbnails = [...gallery.querySelectorAll('[data-gallery-thumbnail]')];
		const currentIndex = thumbnails.indexOf(thumbnail);
		const direction = event.key === 'ArrowRight' ? 1 : -1;
		const nextIndex = (currentIndex + direction + thumbnails.length) % thumbnails.length;
		event.preventDefault();
		thumbnails[nextIndex].focus();
		selectGalleryImage(thumbnails[nextIndex]);
	});

	const detailBuyForm = document.querySelector('#detail-buy-form');
	const stickyAction = document.querySelector('[data-sticky-action]');
	const siteFooter = document.querySelector('.site-footer');
	const relatedProducts = document.querySelector('.related-products');
	if (detailBuyForm && stickyAction) {
		let hasPassedBuyForm = false;
		const defaultStickyBottom = Number.parseFloat(getComputedStyle(stickyAction).bottom) || 16;
		const updateStickyAction = () => {
			const bounds = detailBuyForm.getBoundingClientRect();
			const footerBounds = siteFooter?.getBoundingClientRect();
			const stickyButton = stickyAction.querySelector('button');
			const stickyHeight = stickyButton?.offsetHeight || 52;
			const stickyTop = window.innerHeight - defaultStickyBottom - stickyHeight;
			if (bounds.bottom <= 0) hasPassedBuyForm = true;
			else if (bounds.top >= window.innerHeight) hasPassedBuyForm = false;
			const footerVisible = footerBounds && footerBounds.top < window.innerHeight && footerBounds.bottom > 0;
			const footerOverlapsAction = footerVisible && footerBounds.top < stickyTop;
			if (footerOverlapsAction) {
				stickyAction.style.bottom = `${window.innerHeight - footerBounds.top + 12}px`;
			} else {
				stickyAction.style.removeProperty('bottom');
			}
			const footerTooClose = footerVisible && footerBounds.top < stickyHeight + 32;
			const actionBounds = stickyAction.getBoundingClientRect();
			const relatedBounds = relatedProducts?.getBoundingClientRect();
			const relatedProductsOverlapAction = relatedBounds
				&& relatedBounds.top < actionBounds.bottom
				&& relatedBounds.bottom > actionBounds.top;
			stickyAction.hidden = !hasPassedBuyForm || bounds.bottom > 0 || footerTooClose || relatedProductsOverlapAction;
		};
		updateStickyAction();
		window.addEventListener('scroll', updateStickyAction, { passive: true });
		window.addEventListener('resize', updateStickyAction);

		if ('IntersectionObserver' in window) {
			const buyFormObserver = new IntersectionObserver(updateStickyAction, { threshold: 0.1 });
			buyFormObserver.observe(detailBuyForm);
		}

		stickyAction.querySelector('button')?.addEventListener('click', () => detailBuyForm.requestSubmit());
	}

	const cards = [...document.querySelectorAll('#collection .product-card')];
	const catalogHeader = document.querySelector('#new-arrivals');
	const catalogTitle = catalogHeader?.querySelector('h2');
	const catalogCount = document.querySelector('.catalog-count');
	const filterTitles = { all: 'Utvalgte produkter', new: 'Nyheter', sale: 'Tilbud' };
	const tabs = [...document.querySelectorAll('.category-tab')];
	const categoryLinks = [...document.querySelectorAll('[data-category-link]')];
	const productFilterLinks = [...document.querySelectorAll('[data-product-filter]')];
	const search = document.querySelector('#product-search');
	const emptyState = document.querySelector('.no-results');
	const showCartToast = message => {
		document.querySelector('.ajax-cart-toast')?.remove();
		const toast = document.createElement('div');
		toast.className = 'alert alert-success action-toast ajax-cart-toast';
		toast.setAttribute('role', 'status');
		toast.innerHTML = '<span class="feedback-icon" aria-hidden="true">✓</span><span></span><button class="toast-close" type="button" aria-label="Lukk melding">×</button>';
		toast.querySelector('span:nth-child(2)').textContent = message;
		document.body.appendChild(toast);
		const dismiss = () => toast.remove();
		toast.querySelector('.toast-close')?.addEventListener('click', dismiss);
		window.setTimeout(dismiss, 3000);
	};

	let selectedCategory = 'all';
	let selectedFilter = 'all';
	if (!cards.length) return;

	const updateProducts = () => {
		const term = (search?.value || '').trim().toLowerCase();
		let visible = 0;

		cards.forEach(card => {
			const matchesCategory = selectedCategory === 'all' || card.dataset.category === selectedCategory;
			const matchesFilter = selectedFilter === 'all'
				|| (selectedFilter === 'new' && card.dataset.badge === 'Ny')
				|| (selectedFilter === 'sale' && card.dataset.sale === 'true');
			const matchesSearch = !term || (card.dataset.search || '').toLowerCase().includes(term);
			const shouldShow = matchesCategory && matchesFilter && matchesSearch;
			card.hidden = !shouldShow;
			if (shouldShow) visible++;
		});

		if (emptyState) emptyState.hidden = visible !== 0;
		if (catalogTitle) catalogTitle.textContent = filterTitles[selectedFilter] || filterTitles.all;
		if (catalogCount) catalogCount.textContent = `${visible} varer`;
	};

	tabs.forEach(tab => tab.addEventListener('click', () => {
		selectedFilter = 'all';
		selectedCategory = tab.dataset.category || 'all';
		tabs.forEach(item => item.classList.toggle('active', item === tab));
		updateProducts();
	}));

	search?.addEventListener('input', updateProducts);

	categoryLinks.forEach(link => link.addEventListener('click', () => {
		selectedFilter = 'all';
		const tab = tabs.find(item => item.dataset.category === link.dataset.categoryLink);
		tab?.click();
	}));

	const applyProductFilter = filter => {
		selectedFilter = filter || 'all';
		selectedCategory = 'all';
		tabs.forEach(item => item.classList.toggle('active', item.dataset.category === 'all'));
		updateProducts();
	};

	productFilterLinks.forEach(link => link.addEventListener('click', event => {
		event.preventDefault();
		catalogHeader?.scrollIntoView({ behavior: 'smooth' });
		applyProductFilter(link.dataset.productFilter);
	}));

	const hash = window.location.hash;
	if (hash === '#new-arrivals') applyProductFilter('new');
	if (hash === '#offers') {
		applyProductFilter('sale');
		catalogHeader?.scrollIntoView();
	}

	document.querySelectorAll('.cart-quantity-form').forEach(form => form.addEventListener('submit', async event => {
		event.preventDefault();
		const control = form.closest('.quantity-control');
		const quantityValue = control?.querySelector('.quantity-value');
		const removeForm = control?.querySelector('.remove-form');
		const submitButton = form.querySelector('button');
		if (!control || !quantityValue || !removeForm || !submitButton) return;

		const isRemove = form.classList.contains('remove-form');
		const currentQuantity = Number.parseInt(quantityValue.textContent || '0', 10) || 0;
		submitButton.disabled = true;

		try {
			const response = await fetch(form.action, {
				method: 'POST',
				body: new FormData(form),
				headers: { 'X-Requested-With': 'XMLHttpRequest' }
			});
			if (!response.ok) throw new Error('Cart update failed');

			const result = await response.json();
			const nextQuantity = Number.isInteger(result.quantity)
				? result.quantity
				: Math.max(0, currentQuantity + (isRemove ? -1 : 1));
			quantityValue.textContent = nextQuantity.toString();
			quantityValue.hidden = nextQuantity === 0;
			removeForm.hidden = nextQuantity === 0;
			const cartBadge = document.querySelector('.nav-badge');
			if (cartBadge && Number.isInteger(result.cartCount)) cartBadge.textContent = result.cartCount.toString();
			showCartToast(isRemove ? (nextQuantity === 0 ? 'Produktet er fjernet fra handlekurven.' : 'Antallet er oppdatert.') : 'Produktet er lagt i handlekurven.');
		} catch {
			window.location.reload();
		} finally {
			submitButton.disabled = false;
		}
	}));
})();
