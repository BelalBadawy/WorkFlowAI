---
name: UI Design Guidelines & Component Building
category: ui-ux
description: Comprehensive guide for designing, styling, and building modern, responsive, accessible, and high-quality UI components in the WFAI React client.
triggers:
  - ui design
  - create component
  - style component
  - frontend layout
  - design system
  - tailwind styling
  - build ui
  - add ui page
---

# UI Design Guidelines & Component Building

## Overview
This skill provides architectural patterns, styling conventions, design tokens, and step-by-step workflows for building premium, responsive, and accessible user interface components in the **WFAI** frontend (`WFAI.Client`). 

The application utilizes **React (TypeScript)**, **Tailwind CSS**, **Lucide React Icons**, **Radix / Shadcn UI Primitives**, and **TanStack React Query**.

Use this guide to ensure visual excellence, component reusability, dark mode compatibility, consistent interaction feedback, and strict adherence to modern UI/UX design standards across the entire application.

---

## Design System & Aesthetic Principles

### 1. Visual Excellence & Aesthetics
- **Never rely on default browser styles or basic unstyled HTML elements.**
- **Color Palette**: Use curated Tailwind color tokens with rich dark mode contrast (`slate`, `zinc`, `neutral`, `indigo`, `violet`, `emerald`, `rose`, `amber`). Avoid harsh primary colors (e.g. pure `#ff0000` or `#0000ff`).
- **Surface & Depth**: Use layered backgrounds, subtle borders (`border-border` or `border-slate-200 dark:border-slate-800`), backdrop blurs (`backdrop-blur-md`), and drop shadows (`shadow-sm`, `shadow-md`, `shadow-xl`) to establish visual depth.
- **Glassmorphism**: For floating panels, navbars, and dialog headers, combine translucent backgrounds (`bg-white/80 dark:bg-slate-900/80`) with `backdrop-blur-lg`.
- **Typography**: Maintain clear hierarchy using proportional text sizes (`text-xs` for badges/captions, `text-sm` for secondary text/labels, `text-base` for body, `text-lg`/`text-xl` for section headers, `text-2xl`/`text-3xl` for page titles) and font weights (`font-medium`, `font-semibold`, `font-bold`).

### 2. Responsiveness & Grid Layouts
- Design **Mobile-First**. Every view must work smoothly across viewports (`sm: 640px`, `md: 768px`, `lg: 1024px`, `xl: 1280px`).
- Use **CSS Grid** for multi-column dashboards (`grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4`).
- Use **Flexbox** for alignment and action rows (`flex items-center justify-between gap-3`).
- Prevent content overflowing with `truncate`, `min-w-0`, or `overflow-x-auto`.

### 3. Animations & Micro-Interactions
- Add smooth transitions to all interactive elements (`transition-all duration-200 ease-in-out`).
- **Buttons & Cards**: Subtle hover lift (`hover:-translate-y-0.5`), scale (`active:scale-[0.98]`), and glow/shadow enhancements (`hover:shadow-md`).
- **Focus Rings**: Always maintain visible keyboard navigation indicators (`focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/50 focus-visible:ring-offset-2`).

---

## Component Architecture & File Organization

Organize UI code logically inside `WFAI.Client/src/`:

```
WFAI.Client/src/
├── components/
│   ├── ui/                   # Atomic base components (Button, Input, Card, Dialog, Badge, Table, Tabs)
│   ├── common/               # Shared layout & structural elements (PageHeader, Sidebar, Header, LoadingSpinner, EmptyState)
│   └── features/             # Domain-specific UI features (e.g. categories, products, orders)
│       └── categories/
│           ├── CategoryForm.tsx
│           ├── CategoryCard.tsx
│           └── CategoryTable.tsx
├── pages/                    # Top-level page components (CategoriesManagement.tsx, Dashboard.tsx)
├── hooks/                    # React Query & UI state hooks (useCategories.ts, useTheme.ts)
└── lib/                      # Utilities & API wrappers (utils.ts, categories-api.ts)
```

---

## Core UI State Guidelines

Every feature UI component must gracefully handle **four primary UI states**:

| State | Best Practice | Anti-Pattern |
| :--- | :--- | :--- |
| **Loading** | Use animated Skeleton loaders (`animate-pulse`) matching the layout shape. | Showing a blank screen or a single unstyled text "Loading...". |
| **Empty** | Render a centered Empty State component with an illustrative icon, friendly description, and primary Action CTA button. | Showing an empty table header or a blank page with no instructions. |
| **Error** | Render an inline Error Alert or Error Boundary with a clear explanation and a "Retry" button. | Silent failure or crashing the app with an unhandled exception. |
| **Success / Data** | Display interactive data grid/cards with responsive pagination, quick search, and action menus. | Overcrowded tables without padding or text wrapping. |

---

## Step-by-Step UI Component Workflow

### Step 1: Define Component Specs & Props
- Define clear, strongly-typed TypeScript interfaces for component props.
- Keep props explicit and extend standard HTML element attributes when building base inputs/buttons (`React.ButtonHTMLAttributes<HTMLButtonElement>`).

### Step 2: Implement Component Structure
- Use semantic HTML tags (`<header>`, `<nav>`, `<main>`, `<section>`, `<article>`, `<aside>`, `<button>`).
- Export reusable components cleanly.

### Step 3: Apply Responsive Styling & Theme Tokens
- Apply Tailwind utility classes ensuring dark mode compatibility (`dark:...`).
- Support theme variance via `clsx` or `tailwind-merge` (`cn(...)` utility helper).

### Step 4: Add Micro-Interactions & Accessibility
- Ensure icon-only buttons have explicit `aria-label` tags.
- Support full keyboard navigation (`Tab`, `Enter`, `Escape` key handlers for dialogs).

### Step 5: Connect State & Handlers
- Integrate with custom React Query hooks for async actions (`isLoading`, `isError`, `mutate`).
- Provide feedback via toast notifications (`toast.success(...)`, `toast.error(...)`).

---

## Reusable Code Templates & Patterns

### 1. Shared Page Header Component (`src/components/common/PageHeader.tsx`)
```tsx
import React from 'react';
import { LucideIcon } from 'lucide-react';

interface PageHeaderProps {
  title: string;
  description?: string;
  icon?: LucideIcon;
  actions?: React.ReactNode;
}

export const PageHeader: React.FC<PageHeaderProps> = ({
  title,
  description,
  icon: Icon,
  actions,
}) => {
  return (
    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 mb-6 border-b border-slate-200 dark:border-slate-800">
      <div className="flex items-center gap-3">
        {Icon && (
          <div className="p-2.5 rounded-xl bg-primary/10 text-primary dark:bg-primary/20">
            <Icon className="w-6 h-6" />
          </div>
        )}
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            {title}
          </h1>
          {description && (
            <p className="text-sm text-slate-500 dark:text-slate-400 mt-0.5">
              {description}
            </p>
          )}
        </div>
      </div>
      {actions && <div className="flex items-center gap-2">{actions}</div>}
    </div>
  );
};
```

### 2. Standard Skeleton Loader (`src/components/common/CardSkeleton.tsx`)
```tsx
import React from 'react';

export const CardSkeleton: React.FC<{ count?: number }> = ({ count = 3 }) => {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      {Array.from({ length: count }).map((_, idx) => (
        <div
          key={idx}
          className="p-5 rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-sm animate-pulse space-y-4"
        >
          <div className="flex justify-between items-center">
            <div className="h-5 w-1/3 bg-slate-200 dark:bg-slate-800 rounded-md" />
            <div className="h-4 w-12 bg-slate-200 dark:bg-slate-800 rounded-full" />
          </div>
          <div className="h-4 w-3/4 bg-slate-200 dark:bg-slate-800 rounded-md" />
          <div className="h-4 w-1/2 bg-slate-200 dark:bg-slate-800 rounded-md" />
          <div className="pt-3 flex justify-end gap-2">
            <div className="h-8 w-16 bg-slate-200 dark:bg-slate-800 rounded-lg" />
            <div className="h-8 w-16 bg-slate-200 dark:bg-slate-800 rounded-lg" />
          </div>
        </div>
      ))}
    </div>
  );
};
```

### 3. Actionable Empty State (`src/components/common/EmptyState.tsx`)
```tsx
import React from 'react';
import { LucideIcon, Inbox } from 'lucide-react';

interface EmptyStateProps {
  title: string;
  description: string;
  icon?: LucideIcon;
  action?: {
    label: string;
    onClick: () => void;
    icon?: LucideIcon;
  };
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  title,
  description,
  icon: Icon = Inbox,
  action,
}) => {
  return (
    <div className="flex flex-col items-center justify-center p-12 text-center rounded-2xl border-2 border-dashed border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900/50">
      <div className="p-4 rounded-full bg-slate-100 dark:bg-slate-800 text-slate-400 dark:text-slate-500 mb-4">
        <Icon className="w-8 h-8" />
      </div>
      <h3 className="text-lg font-semibold text-slate-900 dark:text-slate-100 mb-1">
        {title}
      </h3>
      <p className="text-sm text-slate-500 dark:text-slate-400 max-w-sm mb-6">
        {description}
      </p>
      {action && (
        <button
          onClick={action.onClick}
          className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-primary rounded-xl hover:bg-primary/90 transition-all duration-200 shadow-sm hover:shadow active:scale-[0.98]"
        >
          {action.icon && <action.icon className="w-4 h-4" />}
          {action.label}
        </button>
      )}
    </div>
  );
};
```

---

## UI Verification & Quality Checklist

Before finalizing any UI component or page:

- [ ] **Dark Mode Test**: Verify contrast and readability in both Light and Dark themes.
- [ ] **Responsiveness Test**: Test at mobile (`375px`), tablet (`768px`), and desktop (`1440px`) widths.
- [ ] **Interactive Feedback**: Verify buttons show hover, active, focus, and disabled/loading states.
- [ ] **Loading & Empty State**: Verify skeleton loader renders during query fetch, and Empty State renders when array is empty.
- [ ] **Accessibility (A11y)**: Check that interactive elements use proper tags (`<button>`, `<a>`, `<input>`), icon-only buttons have `aria-label`, and colors pass contrast ratios.
- [ ] **No Hardcoded Pixels**: Use Tailwind responsive spacing utilities (`p-4`, `gap-3`, `max-w-7xl`).
