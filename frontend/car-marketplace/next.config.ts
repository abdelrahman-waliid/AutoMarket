import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  images: {
    remotePatterns: [
      {
        protocol: "http",
        hostname: "localhost",
        port: "5127",
        pathname: "/uploads/**",
      },
    ],
  } ,
  reactCompiler: true,
};

export default nextConfig;
