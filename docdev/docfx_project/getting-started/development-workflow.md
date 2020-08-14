# Development Workflow

We develop with Github using pull requests (see [this Github guide](
https://guides.github.com/introduction/flow/) for a short introduction). 

The development branch is always `master`. The releases mark the development 
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

Please note that running the Github actions consumes limited resources (we have only 2,000 minutes
per month available for CI on our current Github plan). You can manually disable workflows
by appending the following lines to the body of the pull request:
* `The workflow build-test-inspect was intentionally skipped.`
* `The workflow check-style was intentionally skipped.`
* `The workflow check-release was intentionally skipped.`

For an example, see [this pull request](
https://github.com/admin-shell-io/aasx-package-explorer/pull/94
).

## Commit Messages

The commit messages follow the guidelines from 
from https://chris.beams.io/posts/git-commit:

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

## Large Binary Files

We use Git Large File Support (LFS) to handle binary files. Please do not forget
to install Git-Lfs (https://git-lfs.github.com) on your computer.

You need to make sure the files are tracked before you add binaries to the 
repository. Invoking:
```bash
$ git lfs track
```
gives you the list like this one:
```
Listing tracked patterns
    *.png (.gitattributes)
```
