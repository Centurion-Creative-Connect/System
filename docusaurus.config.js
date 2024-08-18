// @ts-check
// Note: type annotations allow type checking and IDEs autocompletion

const lightCodeTheme = require('prism-react-renderer/themes/github');
const darkCodeTheme = require('prism-react-renderer/themes/dracula');

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'Centurion Airsoft Game System',
  tagline: 'Create your own custom airsoft field, in VRChat!',
  favicon: 'img/favicon.ico',

  // Set the production url of your site here
  url: 'https://system.centurioncc.org',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'Centurion-Creative-Connect', // Usually your GitHub org/user name.
  projectName: 'docs-system', // Usually your repo name.

  onBrokenLinks: 'warn',
  onBrokenMarkdownLinks: 'warn',

  // Even if you don't use internalization, you can use this field to set useful
  // metadata like html lang. For example, if your site is Chinese, you may want
  // to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'ja',
    locales: ['ja', 'en'],
  },

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          routeBasePath: '/',
          sidebarPath: require.resolve('./sidebars.js'),
          editUrl: 'https://github.com/Centurion-Creative-Connect/Docs-System/tree/main/',
        },
        blog: false,
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      }),
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      // Replace with your project's social card
      image: 'img/social-card.png',
      navbar: {
        title: 'Centurion Airsoft System',
        logo: {
          alt: 'CenturionCC Logo',
          src: 'img/logo.svg',
          srcDark: 'img/logo_dark.svg',
        },
        items: [
          {href: 'https://centurioncc.org/', label: 'Home', position: 'left'},
          {
            href: 'https://github.com/Centurion-Creative-Connect/System',
            label: 'GitHub',
            position: 'right',
          },
          {
            type: 'localeDropdown',
            position: 'right'
          }
        ],
      },
      footer: {
        style: 'dark',
        links: [
          {
            label: 'Twitter',
            href: 'https://twitter.com/vrsgf_centurion',
          },
          {
            label: 'GitHub',
            href: 'https://github.com/centurion-creative-connect',
          },
        ],
        copyright: `Copyright © ${new Date().getFullYear()} Centurion Creative Connect. Built with Docusaurus.`,
      },
      prism: {
        theme: lightCodeTheme,
        darkTheme: darkCodeTheme,
      },
    }),
};

module.exports = config;
