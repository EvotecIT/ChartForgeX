    // One SVG tail belongs to several semantic relationships. Move that existing tail under the visible active
    // member so selection, hover, scenarios and filtering affect the complete relationship without duplicate ink.
    const sharedTrunkTails = Array.from(wrapper.querySelectorAll('[data-cfx-role="topology-shared-trunk-tail"]'));
    if (sharedTrunkTails.length) {
      const sharedTrunkMembers = new Map();
      wrapper.querySelectorAll('[data-cfx-role="topology-edge"][data-trunk-owner-id]').forEach(edge => {
        const owner = attr(edge, 'data-trunk-owner-id');
        if (!sharedTrunkMembers.has(owner)) sharedTrunkMembers.set(owner, []);
        sharedTrunkMembers.get(owner).push(edge);
      });
      const activeTrunkScore = edge => {
        const classes = edge.classList;
        return (classes.contains('cfx-topology-html-selected') ? 16 : 0) +
          (classes.contains('cfx-topology-html-scenario-step-active') ? 64 : 0) +
          (classes.contains('cfx-topology-html-scenario-preview') ? 32 : 0) +
          (classes.contains('cfx-topology-html-scenario-active') ? 4 : 0) +
          (classes.contains('cfx-topology-html-related') ? 2 : 0) +
          (classes.contains('cfx-topology-html-hover-related') || classes.contains('cfx-topology-html-hovered') ? 1 : 0);
      };
      const updateSharedTrunks = () => {
        sharedTrunkTails.forEach(tail => {
          const owner = attr(tail, 'data-trunk-owner-id');
          const members = sharedTrunkMembers.get(owner) || [];
          let target = null;
          let targetOpacity = -1;
          let targetScore = -1;
          for (const member of members) {
            const style = getComputedStyle(member);
            if (style.display === 'none' || style.visibility === 'hidden') continue;
            // Scenario and host focus can mute a selected member. Keep the tail on the brightest visible route.
            const value = parseFloat(style.opacity);
            const opacity = Number.isFinite(value) ? value : 1;
            const score = activeTrunkScore(member);
            if (!target || opacity > targetOpacity || opacity === targetOpacity &&
              (score > targetScore || score === targetScore && attr(member, 'data-edge-id') === owner)) {
              target = member;
              targetOpacity = opacity;
              targetScore = score;
            }
          }
          if (target && tail.parentElement !== target) target.appendChild(tail);
          const display = target ? '' : 'none';
          if (tail.style.display !== display) tail.style.display = display;
        });
      };
      let sharedTrunkFrame = 0;
      const sharedTrunkObserver = new MutationObserver(() => {
        if (sharedTrunkFrame) return;
        sharedTrunkFrame = requestAnimationFrame(() => {
          sharedTrunkFrame = 0;
          if (!wrapper.isConnected) { sharedTrunkObserver.disconnect(); return; }
          updateSharedTrunks();
        });
      });
      sharedTrunkObserver.observe(wrapper, { attributes: true, attributeFilter: ['class', 'style', 'hidden'], subtree: true });
      updateSharedTrunks();
    }
