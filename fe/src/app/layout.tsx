import type { Metadata } from "next";
import { AppToaster } from "@/components/app-toaster";
import "./globals.css";

export const metadata: Metadata = {
  title: "Dep Test 1",
  description: "Next.js and ASP.NET Core monorepo",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className="h-full antialiased">
      <body className="min-h-full flex flex-col">
        {children}
        <AppToaster />
      </body>
    </html>
  );
}
