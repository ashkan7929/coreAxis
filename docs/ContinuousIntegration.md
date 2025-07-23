# Continuous Integration in CoreAxis

This document describes the continuous integration (CI) setup for the CoreAxis platform.

## GitHub Actions Workflow

CoreAxis uses GitHub Actions for continuous integration. The workflow is defined in the `.github/workflows/ci.yml` file.

### Workflow Triggers

The workflow is triggered on:

- Push to the `main` branch
- Pull requests targeting the `main` branch

### Workflow Steps

The workflow consists of the following steps:

1. **Checkout Code**: Checks out the repository code.
2. **Setup .NET**: Sets up the .NET SDK.
3. **Restore Dependencies**: Restores NuGet packages.
4. **Build**: Builds the solution.
5. **Run Tests**: Runs unit tests with code coverage.
6. **Upload Coverage Reports**: Uploads code coverage reports to Codecov.
7. **Generate API Documentation**: Generates API documentation using DocFX.
8. **Publish API Documentation**: Publishes API documentation to GitHub Pages.
9. **Publish Modules**: Publishes modules as artifacts.

## Code Coverage

Code coverage is measured using Coverlet and uploaded to Codecov. This provides visibility into which parts of the codebase are covered by tests.

### Viewing Code Coverage Reports

Code coverage reports can be viewed on the Codecov dashboard. A badge can be added to the repository README to show the current code coverage percentage.

## API Documentation

API documentation is generated using DocFX and published to GitHub Pages. This provides a searchable, browsable documentation site for the CoreAxis API.

### Viewing API Documentation

API documentation can be viewed on GitHub Pages. The URL is typically `https://{username}.github.io/{repository}/`.

## Artifacts

Modules are published as artifacts. These artifacts can be downloaded from the GitHub Actions workflow run page.

## Setting Up Codecov

To set up Codecov:

1. Sign up for a Codecov account at [codecov.io](https://codecov.io/).
2. Link your GitHub repository to Codecov.
3. Add the Codecov token as a secret in your GitHub repository settings.

## Setting Up GitHub Pages

To set up GitHub Pages:

1. Go to your repository settings.
2. Scroll down to the GitHub Pages section.
3. Select the branch and folder to use for GitHub Pages.

## Best Practices

1. **Keep the CI pipeline fast**: A fast CI pipeline provides quick feedback to developers.
2. **Run tests in parallel**: Running tests in parallel can significantly reduce the time it takes to run the test suite.
3. **Use caching**: Caching dependencies can speed up the CI pipeline.
4. **Use matrix builds**: Matrix builds allow you to test against multiple configurations (e.g., different .NET versions).
5. **Monitor CI pipeline health**: Regularly check the CI pipeline to ensure it's running smoothly.

## Future Enhancements

1. **Deployment to staging/production**: Add steps to deploy to staging/production environments.
2. **Integration tests**: Add integration tests to the CI pipeline.
3. **Performance tests**: Add performance tests to the CI pipeline.
4. **Security scanning**: Add security scanning to the CI pipeline.
5. **Dependency updates**: Automatically update dependencies when new versions are available.