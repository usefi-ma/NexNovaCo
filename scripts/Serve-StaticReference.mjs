// Loopback-only reference preview. Serves root HTML and approved assets, never repo metadata.
import http from 'node:http';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('../', import.meta.url));
const types = { '.html': 'text/html', '.css': 'text/css', '.js': 'text/javascript',
    '.json': 'application/json', '.svg': 'image/svg+xml', '.png': 'image/png',
    '.jpg': 'image/jpeg', '.jpeg': 'image/jpeg', '.webp': 'image/webp', '.ico': 'image/x-icon' };
http.createServer(async (request, response) => {
    try {
        const pathname = decodeURIComponent(new URL(request.url, 'http://localhost').pathname).replaceAll('\\', '/');
        if (request.method !== 'GET' || !(/^\/[\w-]+\.html$/.test(pathname) || pathname.startsWith('/assets/'))) throw new Error('Not found');
        const target = path.resolve(root, '.' + pathname);
        if (!target.startsWith(root)) throw new Error('Not found');
        const data = await readFile(target);
        response.writeHead(200, { 'Content-Type': types[path.extname(target)] ?? 'application/octet-stream', 'Cache-Control': 'no-store' });
        response.end(data);
    } catch {
        response.writeHead(404); response.end('Not found');
    }
}).listen(5140, '127.0.0.1', () => console.log('Static reference: http://127.0.0.1:5140/service.html'));
