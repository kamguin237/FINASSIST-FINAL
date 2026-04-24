// Service Worker FinAssist — Push Notifications
self.addEventListener('push', event => {
  if (!event.data) return;

  let data = {};
  try { data = event.data.json(); } catch { data = { titre: 'FinAssist', message: event.data.text() }; }

  const options = {
    body:  data.message ?? '',
    icon:  data.icon ?? '/favicon.svg',
    badge: '/favicon.svg',
    data:  { url: data.url ?? '/notifications' },
    vibrate: [200, 100, 200],
    requireInteraction: false
  };

  event.waitUntil(
    self.registration.showNotification(data.titre ?? 'FinAssist', options)
  );
});

self.addEventListener('notificationclick', event => {
  event.notification.close();
  const url = event.notification.data?.url ?? '/';
  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then(list => {
      const existing = list.find(c => c.url.includes(url));
      if (existing) return existing.focus();
      return clients.openWindow(url);
    })
  );
});
