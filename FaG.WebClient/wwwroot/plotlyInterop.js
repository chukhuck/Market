window.renderPlotly = (divId, data, layout) => {
  if (typeof Plotly === 'undefined') {
    console.error('Plotly is not loaded');
    return;
  }

  const gd = document.getElementById(divId);
  if (!gd) {
    console.error(`Element with id=${divId} not found`);
    return;
  }

  Plotly.newPlot(gd, data, layout, {responsive: true});
};