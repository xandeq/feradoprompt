import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: 'export', // Habilita static export para Next.js 13+
  trailingSlash: true, // Compatibilidade com servidores estáticos
  images: {
    unoptimized: true // Necessário para export estático
  },
  env: {
    NEXT_PUBLIC_API_BASE_URL: process.env.NEXT_PUBLIC_API_BASE_URL || 'https://api.feradoprompt.com.br'
  }
};

export default nextConfig;
