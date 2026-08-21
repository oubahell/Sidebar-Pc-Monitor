/*
 * Sidebar Pc Monitor - donation page
 *
 * EDIT ONLY THE OBJECT BELOW. Nothing else on this page needs changing to update
 * your payment details.
 *
 * Anything still reading "YOUR_..." is treated as not configured: the card is
 * shown greyed out with its copy button disabled, so nobody can copy a
 * placeholder and send money into the void.
 *
 * NEVER put a seed phrase, private key, exchange password or API secret in this
 * file. It is public. Only ever put addresses you are happy for the whole world
 * to send money to.
 */
const DONATION_CONFIG = {

  // ---- Iraq -------------------------------------------------------------
  fib: {
    label: 'FIB',
    sublabel: 'First Iraqi Bank',
    value: 'PIIR-OHT3-ZI4Z',
    hint: 'Open FIB → Send → enter this code.',
    /*
     * Save your FIB QR screenshot as docs/assets/img/fib-qr.png and set this to
     * 'assets/img/fib-qr.png'. The card then shows it under the code.
     *
     * Left null on purpose: a QR generated from the code above would scan to a
     * plain string, which the FIB app does not treat as a payment. Only FIB's
     * own QR carries the payload it expects.
     */
    image: null
  },

  zainCash: {
    label: 'ZainCash',
    sublabel: 'Mobile wallet',
    value: 'YOUR_ZAINCASH_NUMBER',
    hint: 'Send to this ZainCash number.',
    image: null
  },

  qi: {
    label: 'Qi Card',
    sublabel: 'Qi wallet ID',
    value: '7118076988',
    hint: 'Use this Qi ID to send money.',
    image: null
  },

  // ---- International ----------------------------------------------------
  bitcoin: {
    label: 'Bitcoin',
    ticker: 'BTC',
    address: 'YOUR_BTC_ADDRESS',
    network: 'Bitcoin',
    // Produces a bitcoin: payment URI in the QR, which every wallet understands.
    uriScheme: 'bitcoin',
    warning: 'Send BTC using the Bitcoin network only.'
  },

  /*
   * USDT exists on several blockchains and they are NOT interchangeable. Sending
   * on the wrong one loses the money permanently, so each network is listed as
   * its own entry with its own address rather than hidden behind one label.
   */
  usdt: [
    {
      label: 'USDT',
      ticker: 'USDT',
      address: 'THDNsX4p5j5ztzQVPjbps4LDce7UzGNyTH',
      network: 'TRC20',
      networkLong: 'Tron (TRC20)',
      warning: 'This address is Tron (TRC20) only. USDT sent on any other network will be lost.'
    },
    {
      label: 'USDT',
      ticker: 'USDT',
      address: '0xc8ffd9735587782e3cabbd093c49fc8f1054e8bf',
      network: 'BEP20',
      networkLong: 'BNB Smart Chain (BEP20)',
      warning: 'This address is BNB Smart Chain (BEP20) only. USDT sent on any other network will be lost.'
    }
  ]
};

/* ------------------------------------------------------------------------ *
 * Nothing below here needs editing.
 * ------------------------------------------------------------------------ */

(function () {
  'use strict';

  /** A value nobody has filled in yet. */
  function isPlaceholder(value) {
    return typeof value !== 'string' || value.trim() === '' || /^YOUR_/.test(value);
  }

  function el(tag, className, text) {
    var node = document.createElement(tag);
    if (className) { node.className = className; }
    if (text !== undefined && text !== null) { node.textContent = text; }
    return node;
  }

  /* -------------------------------------------------------- QR rendering */

  /**
   * Draws a QR code into a container.
   *
   * Rendered as SVG so it stays sharp at any size and when printed, and on a
   * white plate because that is what scanners expect - a dark-on-dark QR reads
   * poorly on a lot of phone cameras.
   */
  function renderQR(container, text) {
    container.innerHTML = '';

    if (!text) { return false; }

    try {
      // 0 = pick the smallest version that fits. 'M' recovers ~15% damage,
      // which is the usual choice for addresses shown on a screen.
      var qr = qrcode(0, 'M');
      qr.addData(text);
      qr.make();
      container.innerHTML = qr.createSvgTag({ cellSize: 6, margin: 2, scalable: true });
      var svg = container.querySelector('svg');
      if (svg) {
        svg.setAttribute('role', 'img');
        svg.setAttribute('aria-label', 'QR code for the address shown beside it');
        svg.removeAttribute('width');
        svg.removeAttribute('height');
      }
      return true;
    } catch (err) {
      // Better an honest gap than a wrong QR on a page about sending money.
      container.appendChild(el('p', 'qr-failed', 'QR unavailable — please copy the address instead.'));
      return false;
    }
  }

  /* ------------------------------------------------------------ clipboard */

  var toastTimer = null;

  function toast(message, tone) {
    var node = document.getElementById('toast');
    node.textContent = message;
    node.className = 'toast is-visible' + (tone ? ' toast--' + tone : '');

    window.clearTimeout(toastTimer);
    toastTimer = window.setTimeout(function () {
      node.className = 'toast';
    }, 2600);
  }

  function copyText(text) {
    if (navigator.clipboard && window.isSecureContext) {
      return navigator.clipboard.writeText(text);
    }

    // file:// and plain http have no async clipboard. Fall back rather than fail.
    return new Promise(function (resolve, reject) {
      var area = document.createElement('textarea');
      area.value = text;
      area.setAttribute('readonly', '');
      area.style.position = 'fixed';
      area.style.top = '-1000px';
      document.body.appendChild(area);
      area.select();

      var ok = false;
      try { ok = document.execCommand('copy'); } catch (e) { ok = false; }
      document.body.removeChild(area);
      if (ok) { resolve(); } else { reject(new Error('copy failed')); }
    });
  }

  function wireCopyButton(button, getText, label) {
    button.addEventListener('click', function () {
      var text = getText();

      if (isPlaceholder(text)) {
        toast('Not set up yet — nothing to copy.', 'warn');
        return;
      }

      copyText(text).then(function () {
        button.classList.add('is-copied');
        var original = button.querySelector('.copy-label');
        if (original) { original.textContent = 'Copied!'; }

        toast(label + ' copied to clipboard.', 'ok');

        window.setTimeout(function () {
          button.classList.remove('is-copied');
          if (original) { original.textContent = 'Copy'; }
        }, 2000);
      }).catch(function () {
        toast('Could not copy — select the text and copy it manually.', 'warn');
      });
    });
  }

  /* ------------------------------------------------------- Iraq payments */

  function buildLocalCard(config) {
    var configured = !isPlaceholder(config.value);

    var card = el('article', 'card' + (configured ? '' : ' card--unset'));

    var head = el('div', 'card__head');
    var titles = el('div');
    titles.appendChild(el('h3', 'card__title', config.label));
    titles.appendChild(el('p', 'card__sub', config.sublabel));
    head.appendChild(titles);

    if (!configured) {
      head.appendChild(el('span', 'badge badge--muted', 'Not set up yet'));
    }

    card.appendChild(head);

    var valueRow = el('div', 'value');
    var value = el('code', 'value__text', configured ? config.value : 'Not available yet');
    value.setAttribute('aria-label', config.label + ' payment details');
    valueRow.appendChild(value);

    var copy = el('button', 'btn btn--copy');
    copy.type = 'button';
    copy.setAttribute('aria-label', 'Copy ' + config.label + ' details');
    copy.appendChild(el('span', 'copy-label', 'Copy'));
    if (!configured) { copy.disabled = true; }
    valueRow.appendChild(copy);

    card.appendChild(valueRow);

    if (config.hint) {
      card.appendChild(el('p', 'card__hint', config.hint));
    }

    /*
     * A QR is shown for a local method only when a real screenshot is supplied.
     *
     * Generating one from the account number would produce a code that scans to
     * a bare string, which the FIB and Qi apps do not accept as a payment - it
     * would look official and silently not work. Their own apps produce a QR
     * carrying the payload they expect; that image is the only correct one.
     */
    if (configured && config.image) {
      var plate = el('div', 'qr-plate qr-plate--small');
      var img = document.createElement('img');
      img.src = config.image;
      img.alt = config.label + ' payment QR code';
      img.loading = 'lazy';
      plate.appendChild(img);
      card.appendChild(plate);
    }

    if (configured) {
      wireCopyButton(copy, function () { return config.value; }, config.label);
    }

    return card;
  }

  /* ------------------------------------------------------------- crypto */

  var cryptoOptions = [];
  var activeCrypto = 0;

  function paymentUri(option) {
    if (option.uriScheme && !isPlaceholder(option.address)) {
      return option.uriScheme + ':' + option.address;
    }
    // Token networks have no universally understood URI scheme. A bare address
    // is what every wallet accepts, and inventing a scheme risks a wallet
    // silently picking the wrong chain.
    return option.address;
  }

  function showCrypto(index) {
    activeCrypto = index;
    var option = cryptoOptions[index];
    var configured = !isPlaceholder(option.address);

    document.querySelectorAll('.chip').forEach(function (chip, i) {
      var on = i === index;
      chip.classList.toggle('is-active', on);
      chip.setAttribute('aria-selected', on ? 'true' : 'false');
      chip.tabIndex = on ? 0 : -1;
    });

    document.getElementById('crypto-name').textContent = option.label;
    document.getElementById('crypto-ticker').textContent = option.ticker;

    var badge = document.getElementById('crypto-network');
    badge.textContent = option.networkLong || option.network;

    document.getElementById('crypto-warning').textContent = option.warning;

    var address = document.getElementById('crypto-address');
    address.textContent = configured ? option.address : 'Not available yet';
    address.classList.toggle('value__text--unset', !configured);

    var copy = document.getElementById('crypto-copy');
    copy.disabled = !configured;
    copy.setAttribute('aria-label', 'Copy ' + option.label + ' ' + option.network + ' address');

    var plate = document.getElementById('crypto-qr');
    if (configured) {
      renderQR(plate, paymentUri(option));
      plate.classList.remove('is-empty');
    } else {
      plate.innerHTML = '';
      plate.appendChild(el('p', 'qr-failed', 'Address not set up yet.'));
      plate.classList.add('is-empty');
    }
  }

  function buildCryptoChips() {
    var rail = document.getElementById('crypto-chips');

    cryptoOptions.forEach(function (option, index) {
      var chip = el('button', 'chip');
      chip.type = 'button';
      chip.setAttribute('role', 'tab');
      chip.id = 'chip-' + index;
      chip.tabIndex = index === 0 ? 0 : -1;
      chip.setAttribute('aria-selected', index === 0 ? 'true' : 'false');

      chip.appendChild(el('span', 'chip__name', option.ticker));
      chip.appendChild(el('span', 'chip__net', option.network));

      chip.addEventListener('click', function () { showCrypto(index); });
      rail.appendChild(chip);
    });

    // Left/right arrows move between currencies, which is what a screen reader
    // user expects from a tablist.
    rail.addEventListener('keydown', function (e) {
      var next = null;
      if (e.key === 'ArrowRight') { next = (activeCrypto + 1) % cryptoOptions.length; }
      if (e.key === 'ArrowLeft') { next = (activeCrypto - 1 + cryptoOptions.length) % cryptoOptions.length; }
      if (e.key === 'Home') { next = 0; }
      if (e.key === 'End') { next = cryptoOptions.length - 1; }
      if (next === null) { return; }

      e.preventDefault();
      showCrypto(next);
      document.getElementById('chip-' + next).focus();
    });
  }

  /* --------------------------------------------------------------- tabs */

  function wireTabs() {
    var tabs = Array.prototype.slice.call(document.querySelectorAll('.tab'));

    function select(index) {
      tabs.forEach(function (tab, i) {
        var on = i === index;
        tab.classList.toggle('is-active', on);
        tab.setAttribute('aria-selected', on ? 'true' : 'false');
        tab.tabIndex = on ? 0 : -1;

        var panel = document.getElementById(tab.getAttribute('aria-controls'));
        panel.hidden = !on;
      });
    }

    tabs.forEach(function (tab, index) {
      tab.addEventListener('click', function () { select(index); });
    });

    document.getElementById('tablist').addEventListener('keydown', function (e) {
      var current = tabs.findIndex(function (t) { return t.getAttribute('aria-selected') === 'true'; });
      var next = null;
      if (e.key === 'ArrowRight') { next = (current + 1) % tabs.length; }
      if (e.key === 'ArrowLeft') { next = (current - 1 + tabs.length) % tabs.length; }
      if (next === null) { return; }

      e.preventDefault();
      select(next);
      tabs[next].focus();
    });

    // Deep links: donate.html#international opens straight on that tab, so the
    // app (or anyone else) can point at one without the user hunting for it.
    function fromHash() {
      var hash = (window.location.hash || '').toLowerCase();
      if (hash.indexOf('intern') > -1 || hash.indexOf('crypto') > -1) { return 1; }
      return 0;
    }

    select(fromHash());
    window.addEventListener('hashchange', function () { select(fromHash()); });
  }

  /* --------------------------------------------------------------- init */

  function init() {
    var localGrid = document.getElementById('local-grid');
    [DONATION_CONFIG.fib, DONATION_CONFIG.zainCash, DONATION_CONFIG.qi].forEach(function (config) {
      localGrid.appendChild(buildLocalCard(config));
    });

    cryptoOptions = [DONATION_CONFIG.bitcoin].concat(DONATION_CONFIG.usdt || []);
    buildCryptoChips();

    wireCopyButton(
      document.getElementById('crypto-copy'),
      function () { return cryptoOptions[activeCrypto].address; },
      'Address'
    );

    // Open on the first currency that actually has an address. Landing on one
    // that reads "not set up yet" makes a donation page look broken, and the
    // visitor has no way of knowing the others are fine.
    var firstReady = 0;
    for (var i = 0; i < cryptoOptions.length; i++) {
      if (!isPlaceholder(cryptoOptions[i].address)) { firstReady = i; break; }
    }

    showCrypto(firstReady);
    wireTabs();

    document.getElementById('year').textContent = String(new Date().getFullYear());
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
