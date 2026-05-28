const CACHE = 'sqlite-framework-docs-v1'

self.addEventListener('install', () => {
    self.skipWaiting()
})

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys()
            .then(keys => Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k))))
            .then(() => self.clients.claim())
    )
})

self.addEventListener('fetch', event => {
    const request = event.request
    if (request.method !== 'GET') return
    const url = new URL(request.url)
    if (url.origin !== self.location.origin) return

    event.respondWith(
        caches.open(CACHE).then(async cache => {
            const cached = await cache.match(request)
            const network = fetch(request).then(response => {
                if (response && response.ok && response.type === 'basic') {
                    cache.put(request, response.clone())
                }
                return response
            }).catch(() => cached)
            return cached || network
        })
    )
})
