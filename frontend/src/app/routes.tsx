import { lazy, Suspense, type ReactNode } from "react";
import { createBrowserRouter } from "react-router-dom";
import { UserMenu } from "@/features/auth/components/UserMenu";
import { NotificationBell } from "@/features/notifications/components/NotificationBell";
import { GuestRoute } from "@/app/GuestRoute";
import { ProtectedRoute } from "@/app/ProtectedRoute";
import { RoleRedirect } from "@/app/pages/RoleRedirect";
import { FullPageSpinner } from "@/shared/components";
import { AdminLayout } from "@/shared/layouts/AdminLayout";
import { CustomerLayout } from "@/shared/layouts/CustomerLayout";
import { STAFF_ROLES, UserRoles } from "@/shared/types/auth.types";

// Route bazlı code splitting.
const LandingPage = lazy(() => import("@/app/pages/LandingPage"));
const LoginPage = lazy(() => import("@/features/auth/pages/LoginPage"));
const RegisterPage = lazy(() => import("@/features/auth/pages/RegisterPage"));
const ForgotPasswordPage = lazy(() => import("@/features/auth/pages/ForgotPasswordPage"));
const ResetPasswordPage = lazy(() => import("@/features/auth/pages/ResetPasswordPage"));
const PortalHomePage = lazy(() => import("@/app/pages/PortalHomePage"));
const ProfilePage = lazy(() => import("@/features/profile/pages/ProfilePage"));
const QuoteListPage = lazy(() => import("@/features/quotes/pages/QuoteListPage"));
const QuoteWizardPage = lazy(() => import("@/features/quotes/pages/QuoteWizardPage"));
const QuoteDetailPage = lazy(() => import("@/features/quotes/pages/QuoteDetailPage"));
const PurchasePage = lazy(() => import("@/features/payments/pages/PurchasePage"));
const PolicyListPage = lazy(() => import("@/features/policies/pages/PolicyListPage"));
const PolicyDetailPage = lazy(() => import("@/features/policies/pages/PolicyDetailPage"));
const ClaimListPage = lazy(() => import("@/features/claims/pages/ClaimListPage"));
const ClaimCreatePage = lazy(() => import("@/features/claims/pages/ClaimCreatePage"));
const ClaimDetailPage = lazy(() => import("@/features/claims/pages/ClaimDetailPage"));
const RenewalListPage = lazy(() => import("@/features/renewals/pages/RenewalListPage"));
const AdminDashboardPage = lazy(() => import("@/features/dashboard/pages/AdminDashboardPage"));
const AdminCustomerListPage = lazy(() => import("@/features/customers/pages/AdminCustomerListPage"));
const AdminCustomerQuoteWizardPage = lazy(
  () => import("@/features/quotes/pages/AdminCustomerQuoteWizardPage"),
);
const AdminQuoteListPage = lazy(() => import("@/features/quotes/pages/AdminQuoteListPage"));
const AdminPolicyListPage = lazy(() => import("@/features/policies/pages/AdminPolicyListPage"));
const AdminClaimListPage = lazy(() => import("@/features/claims/pages/AdminClaimListPage"));
const NotificationCenterPage = lazy(() => import("@/features/notifications/pages/NotificationCenterPage"));
const AdminStaffListPage = lazy(() => import("@/features/staff/pages/AdminStaffListPage"));
const AdminPricingPage = lazy(() => import("@/features/pricing/pages/AdminPricingPage"));
const UnauthorizedPage = lazy(() => import("@/app/pages/UnauthorizedPage"));
const ForbiddenPage = lazy(() => import("@/app/pages/ForbiddenPage"));
const NotFoundPage = lazy(() => import("@/app/pages/NotFoundPage"));

function withSuspense(node: ReactNode): ReactNode {
  return <Suspense fallback={<FullPageSpinner />}>{node}</Suspense>;
}

/**
 * Uygulama rota ağacı:
 * - /login, /register → anonim (oturum varsa role göre yönlendirilir — GuestRoute).
 * - /portal → müşteri portalı (Customer), /admin → acente paneli (Admin/Personel).
 * - /401 (oturum düştü — axios interceptor hedefi), /403 (rol yetkisiz), * → 404.
 * Kullanıcı menüsü, shared → features bağımlılığı oluşmaması için layout'lara
 * buradan slot olarak verilir.
 */
export const router = createBrowserRouter([
  {
    path: "/",
    element: <RoleRedirect landing={withSuspense(<LandingPage />)} />,
  },
  {
    path: "/login",
    element: <GuestRoute>{withSuspense(<LoginPage />)}</GuestRoute>,
  },
  {
    path: "/register",
    element: <GuestRoute>{withSuspense(<RegisterPage />)}</GuestRoute>,
  },
  {
    path: "/forgot-password",
    element: <GuestRoute>{withSuspense(<ForgotPasswordPage />)}</GuestRoute>,
  },
  {
    path: "/reset-password",
    element: <GuestRoute>{withSuspense(<ResetPasswordPage />)}</GuestRoute>,
  },
  {
    path: "/portal",
    element: (
      <ProtectedRoute allowedRoles={[UserRoles.Customer]}>
        <CustomerLayout userMenu={<UserMenu />} />
      </ProtectedRoute>
    ),
    children: [
      {
        index: true,
        element: withSuspense(<PortalHomePage />),
      },
      {
        path: "profile",
        element: withSuspense(<ProfilePage />),
      },
      {
        path: "quotes",
        element: withSuspense(<QuoteListPage />),
      },
      {
        path: "quotes/new",
        element: withSuspense(<QuoteWizardPage />),
      },
      {
        path: "quotes/:id",
        element: withSuspense(<QuoteDetailPage />),
      },
      {
        path: "quotes/:id/purchase",
        element: withSuspense(<PurchasePage />),
      },
      {
        path: "policies",
        element: withSuspense(<PolicyListPage />),
      },
      {
        path: "policies/:id",
        element: withSuspense(<PolicyDetailPage />),
      },
      {
        path: "claims",
        element: withSuspense(<ClaimListPage />),
      },
      {
        path: "claims/new",
        element: withSuspense(<ClaimCreatePage />),
      },
      {
        path: "claims/:id",
        element: withSuspense(<ClaimDetailPage />),
      },
      {
        path: "renewals",
        element: withSuspense(<RenewalListPage />),
      },
    ],
  },
  {
    path: "/admin",
    element: (
      <ProtectedRoute allowedRoles={STAFF_ROLES}>
        <AdminLayout userMenu={<UserMenu />} headerExtras={<NotificationBell />} />
      </ProtectedRoute>
    ),
    children: [
      {
        index: true,
        element: withSuspense(<AdminDashboardPage />),
      },
      {
        path: "customers",
        element: withSuspense(<AdminCustomerListPage />),
      },
      {
        // Acente destekli teklif: personel/admin, seçili müşteri adına ortak teklif sihirbazını çalıştırır.
        path: "customers/:customerId/quotes/new",
        element: withSuspense(<AdminCustomerQuoteWizardPage />),
      },
      {
        path: "quotes",
        element: withSuspense(<AdminQuoteListPage />),
      },
      {
        path: "policies",
        element: withSuspense(<AdminPolicyListPage />),
      },
      {
        path: "claims",
        element: withSuspense(<AdminClaimListPage />),
      },
      {
        path: "notifications",
        element: withSuspense(<NotificationCenterPage />),
      },
      {
        // Personel yönetimi yalnızca Admin'e açıktır (personel/müşteri erişemez).
        path: "staff",
        element: (
          <ProtectedRoute allowedRoles={[UserRoles.Admin]}>
            {withSuspense(<AdminStaffListPage />)}
          </ProtectedRoute>
        ),
      },
      {
        // Fiyatlandırma yönetimini acente personeli GÖRÜNTÜLER; yalnızca Admin DEĞİŞTİRİR (taslak
        // oluştur/düzenle/aktifleştir). Yazma yetkisi backend'de Admin'e kilitli; sayfa personel için salt-okunur.
        path: "pricing",
        element: withSuspense(<AdminPricingPage />),
      },
    ],
  },
  {
    path: "/401",
    element: withSuspense(<UnauthorizedPage />),
  },
  {
    path: "/403",
    element: withSuspense(<ForbiddenPage />),
  },
  {
    path: "*",
    element: withSuspense(<NotFoundPage />),
  },
]);
