// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

// The markup the site's data files use in their text fields, and site-absolute paths under the base path.

const base = import.meta.env.BASE_URL.replace(/\/?$/, '/');

/** A site-absolute path (`/x/`) as the browser needs it, under the site's base path. */
export function sitePath(path: string): string {
  return base + path.replace(/^\//, '');
}

const escape = (text: string) =>
  text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

/** The text fields' markup: `code`, and [text](href), where a site-absolute href gets the base path. */
export function inline(text: string): string {
  return escape(text)
    .replace(/`([^`]+)`/g, '<code>$1</code>')
    .replace(/\[([^\]]+)\]\(([^)\s]+)\)/g, (_, label: string, href: string) => {
      const target = href.startsWith('/') ? sitePath(href) : href;
      return `<a href="${target}">${label}</a>`;
    });
}
