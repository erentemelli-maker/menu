(function () {
  const indicator = document.querySelector("[data-live-indicator]");
  if (!indicator || typeof signalR === "undefined") {
    return;
  }

  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/orderHub")
    .withAutomaticReconnect()
    .build();

  const updateIndicator = (state, label) => {
    indicator.dataset.state = state;
    indicator.querySelector("span").textContent = label;
  };

  const refreshOrders = () => {
    updateIndicator("refreshing", "Yeni hareket alındı");
    window.setTimeout(() => window.location.reload(), 450);
  };

  connection.on("OrderCreated", refreshOrders);
  connection.on("TableServiceRequested", refreshOrders);
  connection.on("TableServiceResolved", refreshOrders);
  connection.on("OrderStatusChanged", refreshOrders);
  connection.onreconnecting(() => updateIndicator("waiting", "Bağlantı yenileniyor"));
  connection.onreconnected(() => updateIndicator("online", "Canlı bağlantı"));
  connection.onclose(() => updateIndicator("offline", "Bağlantı kapalı"));

  connection.start()
    .then(() => updateIndicator("online", "Canlı bağlantı"))
    .catch(() => updateIndicator("offline", "Bağlantı kurulamadı"));
})();
