import { createRoot } from "react-dom/client";
import {
  AdminPortalDomainVerification,
  AdminPortalSsoConnection,
  ApiKeys,
  OrganizationSwitcher,
  Pipes,
  UserProfile,
  UserSecurity,
  UserSessions,
  UsersManagement,
  WorkOsWidgets
} from "@workos-inc/widgets";
import { WorkOsLocaleProvider } from "@workos-inc/widgets-i18n";

import "@radix-ui/themes/styles.css";
import "@workos-inc/widgets/styles.css";

type WidgetKey =
  | "user-management"
  | "user-profile"
  | "user-sessions"
  | "user-security"
  | "api-keys"
  | "pipes"
  | "admin-portal-domain-verification"
  | "admin-portal-sso-connection"
  | "organization-switcher";

type OrganizationSwitcherClientConfig = {
  redirectTemplate?: string;
  redirectFixedRoute?: string;
  switchEndpoint?: string;
  createOrganizationUrl?: string;
  createOrganizationLabel?: string;
  createOrganizationTarget?: string;
  currentOrganizationId?: string;
  currentOrganizationExternalId?: string;
  preferExternalId?: boolean;
  externalIdMap?: Record<string, string>;
};

type WidgetConfig = {
  widget: WidgetKey;
  widgetType?: WidgetKey;
  authToken: string;
  themeJson?: string;
  elementsJson?: string;
  locale?: string;
  textDirection?: "ltr" | "rtl";
  dialogZIndex?: number;
  currentSessionId?: string;
  organizationSwitcher?: OrganizationSwitcherClientConfig;
};

type SwitchEndpointResponse = {
  redirectUrl?: string;
};

function applyDialogZIndex(dialogZIndex?: number) {
  if (!dialogZIndex || dialogZIndex <= 0) {
    return;
  }

  const styleId = "workos-widget-dialog-zindex-override";
  const css = `
.rt-BaseDialogOverlay,
.rt-BaseDialogContent,
.rt-AlertDialogOverlay,
.rt-AlertDialogContent {
  z-index: ${dialogZIndex} !important;
}
`;

  const existing = document.getElementById(styleId);
  if (existing instanceof HTMLStyleElement) {
    existing.textContent = css;
    return;
  }

  const styleElement = document.createElement("style");
  styleElement.id = styleId;
  styleElement.textContent = css;
  document.head.appendChild(styleElement);
}

function renderWidget(element: HTMLElement, config: WidgetConfig) {
  const widget = config.widget ?? config.widgetType;
  const parsedTheme = config.themeJson ? JSON.parse(config.themeJson) : undefined;
  const parsedElements = config.elementsJson ? JSON.parse(config.elementsJson) : undefined;
  applyDialogZIndex(config.dialogZIndex);
  const safeRender = (node: JSX.Element) => {
    let content: JSX.Element = (
      <WorkOsWidgets
        theme={parsedTheme}
        elements={parsedElements}
        style={{ minHeight: "auto", height: "auto" }}
      >
        {node}
      </WorkOsWidgets>
    );

    if (config.locale) {
      content = <WorkOsLocaleProvider locale={config.locale}>{content}</WorkOsLocaleProvider>;
    }

    if (config.textDirection) {
      content = <div dir={config.textDirection}>{content}</div>;
    }

    createRoot(element).render(content);
  };

  switch (widget) {
    case "user-management":
      safeRender(<UsersManagement authToken={config.authToken} />);
      return;
    case "user-profile":
      safeRender(<UserProfile authToken={config.authToken} />);
      return;
    case "user-security":
      safeRender(<UserSecurity authToken={config.authToken} />);
      return;
    case "api-keys":
      safeRender(<ApiKeys authToken={config.authToken} />);
      return;
    case "pipes":
      safeRender(<Pipes authToken={config.authToken} />);
      return;
    case "admin-portal-domain-verification":
      safeRender(<AdminPortalDomainVerification authToken={config.authToken} />);
      return;
    case "admin-portal-sso-connection":
      safeRender(<AdminPortalSsoConnection authToken={config.authToken} />);
      return;
    case "user-sessions":
      if (!config.currentSessionId) {
        throw new Error("user-sessions requires currentSessionId.");
      }

      safeRender(<UserSessions authToken={config.authToken} currentSessionId={config.currentSessionId} />);
      return;
    case "organization-switcher":
      {
        const organizationConfig = config.organizationSwitcher;
        const createOrganizationTarget = organizationConfig?.createOrganizationTarget ?? "_self";
        const createOrganizationChild = organizationConfig?.createOrganizationUrl ? (
          <a
            href={organizationConfig.createOrganizationUrl}
            target={createOrganizationTarget}
            rel={createOrganizationTarget === "_blank" ? "noopener noreferrer" : undefined}
          >
            {organizationConfig.createOrganizationLabel ?? "Create organization"}
          </a>
        ) : undefined;

      safeRender(
        <OrganizationSwitcher
          authToken={config.authToken}
          switchToOrganization={async ({ organizationId }) => {
            const externalId = organizationConfig?.externalIdMap?.[organizationId];
            const preferredId =
              organizationConfig?.preferExternalId && externalId ? externalId : organizationId;

            if (organizationConfig?.switchEndpoint) {
              const response = await fetch(organizationConfig.switchEndpoint, {
                method: "POST",
                headers: {
                  "Content-Type": "application/json"
                },
                body: JSON.stringify({ organizationId })
              });

              if (!response.ok) {
                throw new Error(`Organization switch failed (${response.status}).`);
              }

              let switchResponse: SwitchEndpointResponse | undefined;
              const contentType = response.headers.get("content-type");
              if (contentType && contentType.includes("application/json")) {
                switchResponse = (await response.json()) as SwitchEndpointResponse;
              }

              if (switchResponse?.redirectUrl) {
                window.location.assign(switchResponse.redirectUrl);
                return;
              }

              window.location.assign(window.location.pathname + window.location.search + window.location.hash);
              return;
            }

            if (organizationConfig?.redirectTemplate) {
              const url = organizationConfig.redirectTemplate
                .replaceAll("{organizationId}", organizationId)
                .replaceAll("{externalId}", preferredId);
              window.location.assign(url);
              return;
            }

            if (organizationConfig?.redirectFixedRoute) {
              window.location.assign(organizationConfig.redirectFixedRoute);
              return;
            }

            window.location.assign(window.location.pathname + window.location.search + window.location.hash);
          }}
        >
          {createOrganizationChild}
        </OrganizationSwitcher>
      );
      return;
      }
    default:
      throw new Error(`Unsupported widget: ${widget ?? "(missing widget/widgetType)"}`);
  }
}

function mountAll() {
  const hosts = document.querySelectorAll<HTMLElement>("[data-workos-widget-host]");

  for (const host of hosts) {
    try {
      const rawConfig = host.dataset.workosWidgetConfig;
      if (!rawConfig) {
        throw new Error("Missing widget config.");
      }

      const config = JSON.parse(rawConfig) as WidgetConfig;
      renderWidget(host, config);
    } catch (error) {
      const message = error instanceof Error ? error.message : "Unknown WorkOS widget error.";
      host.textContent = `WorkOS widget error: ${message}`;
    }
  }
}

mountAll();
