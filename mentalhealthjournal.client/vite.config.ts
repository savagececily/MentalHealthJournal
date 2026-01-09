import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = "mentalhealthjournal.client";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

let httpsEnabled = false;
if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    try {
        if (!fs.existsSync(baseFolder)) {
            fs.mkdirSync(baseFolder, { recursive: true });
        }
        if (0 === child_process.spawnSync('dotnet', [
            'dev-certs',
            'https',
            '--export-path',
            certFilePath,
            '--format',
            'Pem',
            '--no-password',
        ], { stdio: 'inherit', }).status) {
            httpsEnabled = true;
        }
    } catch {
        console.warn('HTTPS certificate creation failed. Running in HTTP mode.');
    }
} else {
    httpsEnabled = true;
}

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7102';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        proxy: {
            '^/weatherforecast': {
                target,
                secure: false
            },
            '^/api': {
                target,
                secure: false
            }
        },
        port: parseInt(env.DEV_SERVER_PORT || '54551'),
        ...(httpsEnabled ? {
            https: {
                key: fs.readFileSync(keyFilePath),
                cert: fs.readFileSync(certFilePath),
            }
        } : {})
    }
})
