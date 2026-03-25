window.gymDash = (function () {
    let charts = [];

    function makeChart(ctx, cfg) {
        if (!ctx) return null;
        return new Chart(ctx, cfg);
    }

    function lineRevenue(labels, data) {
        return {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    data,
                    borderColor: '#3B6FE8',
                    backgroundColor: 'rgba(59,111,232,0.07)',
                    borderWidth: 2.5,
                    pointBackgroundColor: '#3B6FE8',
                    pointRadius: 4,
                    pointHoverRadius: 6,
                    fill: true,
                    tension: 0.35
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false }, tooltip: { callbacks: { label: ctx => ' $' + ctx.raw } } },
                scales: {
                    x: { grid: { color: '#E8EDF5', borderDash: [4, 4] }, ticks: { color: '#94A3B8', font: { size: 11, family: 'DM Sans' } } },
                    y: { grid: { color: '#E8EDF5', borderDash: [4, 4] }, ticks: { color: '#94A3B8', font: { size: 11, family: 'DM Sans' } }, beginAtZero: true }
                }
            }
        };
    }

    function barStatus(labels, data) {
        // Colour each status bar distinctly
        const colorMap = {
            'Scheduled': '#3B6FE8',
            'Ongoing':   '#22C55E',
            'Completed': '#F59E0B',
            'Cancelled': '#94A3B8'
        };
        const backgroundColors = labels.map(l => colorMap[l] ?? '#3B6FE8');
        return {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    data,
                    backgroundColor: backgroundColors,
                    borderRadius: 6,
                    borderSkipped: false
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    x: { grid: { display: false }, ticks: { color: '#94A3B8', font: { size: 11, family: 'DM Sans' } } },
                    y: { grid: { color: '#E8EDF5', borderDash: [4, 4] }, ticks: { color: '#94A3B8', font: { size: 11, family: 'DM Sans' }, stepSize: 1 }, beginAtZero: true }
                }
            }
        };
    }

    function doughnutOccupancy(labels, data) {
        return {
            type: 'doughnut',
            data: {
                labels,
                datasets: [{
                    data,
                    backgroundColor: ['#3B6FE8', '#E2E8F0'],
                    borderWidth: 0,
                    hoverOffset: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '72%',
                plugins: { legend: { display: false }, tooltip: { callbacks: { label: ctx => ctx.raw + '%' } } }
            }
        };
    }

    function dispose() {
        charts.forEach(c => c && c.destroy());
        charts = [];
    }

    function init(data) {
        if (!window.Chart) return;
        dispose();
        charts.push(makeChart(document.getElementById('revenueChart'), lineRevenue(data.Revenue.Labels, data.Revenue.Data)));
        charts.push(makeChart(document.getElementById('bookingsChart'), barStatus(data.Bookings.Labels, data.Bookings.Data)));
        charts.push(makeChart(document.getElementById('occupancyChart'), doughnutOccupancy(data.Occupancy.Labels, data.Occupancy.Data)));
    }

    return { init, dispose };
})();
