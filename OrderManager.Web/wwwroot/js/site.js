// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
	document.querySelectorAll('.action-toast').forEach(toast => {
		const dismiss = () => toast.remove();
		toast.querySelector('.toast-close')?.addEventListener('click', dismiss);
		window.setTimeout(dismiss, 5000);
	});

	const cards = [...document.querySelectorAll('.product-card')];
	const tabs = [...document.querySelectorAll('.category-tab')];
	const categoryLinks = [...document.querySelectorAll('[data-category-link]')];
	const productFilterLinks = [...document.querySelectorAll('[data-product-filter]')];
	const search = document.querySelector('#product-search');
	const emptyState = document.querySelector('.no-results');

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
	};

	tabs.forEach(tab => tab.addEventListener('click', () => {
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

	productFilterLinks.forEach(link => link.addEventListener('click', () => {
		applyProductFilter(link.dataset.productFilter);
	}));

	const hash = window.location.hash;
	if (hash === '#new-arrivals') applyProductFilter('new');
	if (hash === '#offers') applyProductFilter('sale');

	document.querySelector('.newsletter-form')?.addEventListener('submit', event => {
		event.preventDefault();
		const button = event.currentTarget.querySelector('button');
		if (button) button.textContent = 'Takk!';
	});
})();
