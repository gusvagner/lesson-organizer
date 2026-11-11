try {
  var mode = localStorage.getItem('lo-theme');
  if (mode === 'light' || mode === 'dark') {
    document.documentElement.style.colorScheme = mode;
  }
} catch (error) {
  document.documentElement.style.colorScheme = '';
}
