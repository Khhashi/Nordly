// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
	const cards = [...document.querySelectorAll('.product-card')];
	const tabs = [...document.querySelectorAll('.category-tab')];
	const search = document.querySelector('#product-search');
	const emptyState = document.querySelector('.no-results');

	if (!cards.length) return;

	let selectedCategory = 'all';

	const updateProducts = () => {
		const term = (search?.value || '').trim().toLowerCase();
		let visible = 0;

		cards.forEach(card => {
			const matchesCategory = selectedCategory === 'all' || card.dataset.category === selectedCategory;
			const matchesSearch = !term || (card.dataset.search || '').toLowerCase().includes(term);
			const shouldShow = matchesCategory && matchesSearch;
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
})();
