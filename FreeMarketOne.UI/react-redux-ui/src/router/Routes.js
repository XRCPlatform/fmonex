/* eslint-disable import/no-unresolved */
// React
import { lazy } from "react";

// Routes (Non splitted)
import PublicLayout from "Layouts/PublicLayout";
import PrivateLayout from "Layouts/PrivateLayout";

// Routes (Code splitting)
const Home = lazy(() => import("Pages/home/index.js"));
const Markets = lazy(() => import("Pages/markets/index.js"));
const Categories = lazy(() => import("Pages/categories/index.js"));
const Products = lazy(() => import("Pages/products/index.js"));
const Dashboard = lazy(() => import("Pages/dashboard/index.js"));
const Admin = lazy(() => import("Pages/admin/index.js"));

/**
|--------------------------------------------------
| PUBLIC ROUTES
|--------------------------------------------------
*/

export const publicRoutes = [
  {
    id: "home",
    title: "Homepage",
    description: "Homepage section",
    path: "/",
    exact: true,
    component: Home,
    layout: PublicLayout
  },
  {
    id: "markets",
    title: "Markets",
    description: "Markets section",
    path: "markets",
    exact: true,
    component: Markets,
    layout: PublicLayout
  },

  {
    id: "categories",
    title: "Categories",
    description: "Categories section",
    path: "categories",
    exact: true,
    component: Categories,
    layout: PublicLayout
  },
  {
    id: "products",
    title: "Products",
    description: "Products section",
    path: "products",
    exact: true,
    component: Products,
    layout: PublicLayout
  },
  {
    id: "dashboard",
    title: "Dashboard",
    description: "Dashboard section",
    path: "dashboard",
    exact: true,
    component: Dashboard,
    layout: PublicLayout
  }
];

/**
|--------------------------------------------------
| PRIVATE ROUTES
|--------------------------------------------------
*/

export const privateRoutes = [
  {
    id: "admin",
    title: "Dashboard",
    description: "Dashboard section",
    path: "admin",
    component: Admin,
    layout: PrivateLayout
  }
];
