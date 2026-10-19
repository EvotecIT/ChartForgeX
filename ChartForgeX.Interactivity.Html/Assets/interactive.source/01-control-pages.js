  // Dense adapter controls share one bounded page calculation; each control owns its content and actions.
  const chartControlPage = (count, requested, pageSize) => {
    const last = Math.max(0, Math.floor((count - 1) / pageSize));
    const page = Math.max(0, Math.min(last, requested ?? 0));
    const offset = page * pageSize;
    return { page, last, offset, first: count ? offset + 1 : 0, end: Math.min(offset + pageSize, count) };
  };
