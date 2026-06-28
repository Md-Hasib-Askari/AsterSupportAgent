import type { NextConfig } from "next";
import path from "path";

const isProduction = process.env.NODE_ENV === 'production';

const nextConfig: NextConfig = {
  output: isProduction ? 'export' : undefined,
  distDir: isProduction ? 'dist' : undefined,
  assetPrefix: isProduction ? '.' : undefined,
  images: {
    unoptimized: true,
  },
  turbopack: {
    root: path.resolve(__dirname, '../..'),
  },
};

export default nextConfig;
