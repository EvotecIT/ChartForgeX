  // Saved-page Treemap and Sunburst drill views live in the hierarchy feature bundle.
  const navigateStandaloneHierarchy = (root, id) => !!chartFeature('hierarchy')?.navigate(root, chartFeatureKernel, id);
