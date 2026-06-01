(function () {
  const updateTimers = () => {
    document.querySelectorAll("[data-created-at]").forEach((element) => {
      const createdAt = new Date(element.dataset.createdAt);
      const minutes = Math.max(0, Math.floor((Date.now() - createdAt.getTime()) / 60000));
      element.textContent = `${minutes} dk`;
    });
  };

  updateTimers();
  window.setInterval(updateTimers, 30000);
})();
