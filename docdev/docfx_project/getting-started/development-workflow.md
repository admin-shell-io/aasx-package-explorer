# Development Workflow

We develop with Github using pull requests (see [this Github guide](
https://guides.github.com/introduction/flow/) for a short introduction). 

**Development branch**. The development branch is always `master`. 

**Releases**. The releases mark the development 
milestones on the `master` branch with a certain feature completeness.

## Pull Requests

**Feature branches**. We develop using the feature branches, see [this section 
of the Git book](
https://git-scm.com/book/en/v2/Git-Branching-Branching-Workflows
).

If you are a member of the development team, create a feature branch directly
within the repository.

Otherwise, if you are a non-member contributor, fork the repository and create
the feature branch in your forked repository. See [this Github tuturial](
https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/creating-a-pull-request-from-a-fork
) for more guidance. 

**Branch Prefix**. Please prefix the branch with your Github user name 
(*e.g.,* `mristin/Add-some-feature`).

**Continuous Integration**. Github will run the continuous integration (CI) automatically through Github 
actions. The CI includes building the solution, running the test, inspecting
the code *etc.* (see below the section "Pre-merge Checks").

Please note that running the Github actions consumes computational resources
which is often unnecessary if you are certain that some checks are not needed.
For example, there is no need to build the whole solution if you only make a
minor change in a powershell Script unrelated to building. 
You can manually disable workflows by appending the following lines 
to the body of the pull request (corresponding to which checks you want to
disable):

* `The workflow build-test-inspect was intentionally skipped.`
* `The workflow check-style was intentionally skipped.`
* `The workflow check-release was intentionally skipped.`

## Commit Messages

The commit messages follow the guidelines from 
https://chris.beams.io/posts/git-commit:

* Separate subject from body with a blank line
* Limit the subject line to 50 characters
* Capitalize the subject line
* Do not end the subject line with a period
* Use the imperative mood in the subject line
* Wrap the body at 72 characters
* Use the body to explain *what* and *why* (instead of *how*)

We automatically check the commit messages using [opinionated-commit-message](
https://github.com/mristin/opinionated-commit-message
).

Here is an example commit message (from [this pull request](
https://github.com/admin-shell-io/aasx-package-explorer/pull/208
)):

```
Make DownloadSamples.ps1 use default proxy

Using the default proxy is necessary so that DownloadSamples.ps1 can
operate on enterprise networks which restrict the network traffic
through the proxy.

The workflow build-test-inspect was intentionally skipped.
The workflow check-style was intentionally skipped.
The workflow check-release was intentionally skipped.
```
