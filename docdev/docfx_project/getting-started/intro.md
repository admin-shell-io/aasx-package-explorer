# Introduction

This series of articles help you set up and build the solution,
explain you how to test and check your code contribution and
finally how to properly submit it.

If you don't like reading the documentation, just want to take a deep dive and
start contributing, the following section "Quick Start" gives you a brief 
overview of how you can get your code in.

## Quick Start

This is a brief list of steps explaining how to submit your code contribution.

### Development Tools

* Install the IDE of your choice, *e.g.*, 
  [Visual Studio 2019 Community Edition][visual-studio].

### Create a feature branch

**If you are a member of [admin-shell-io GitHub organization][organization]**:
 
* Clone the Git repository:
  ```
  git clone https://github.com/admin-shell-io/aasx-package-explorer
  ```
  
* Create your feature branch:

  ```
  git checkout -b yourUsername/Add-some-new-feature
  ``` 
  
  Please observe [our guideline to naming the branches][branches-guideline] 
  (`{your-username}/{Describe-subject-of-the-commit}`).

**Otherwise**:

* Make the fork of your repository (see [this GitHub guide][forking])

* Clone the Git repository:
  ```
  git clone https://github.com/yourUsername/aasx-package-explorer
  ```

* Create your feature branch:

  ```
  git checkout -b yourUsername/Add-some-new-feature
  ``` 

### Dependencies

* Change to the directory of your repository. Execute in Powershell:

  ```
  .\src\InstallSolutionDependencies.ps1
  ```

### Write Your Code

* Make your code changes. 
* Do not forget to implement unit tests.

### Commit & Push

* Format your code to conform to the style guide:

  ```
  .\src\FormatCode.ps1
  ```

* Add files that you would like in your pull request:

  ```
  git add src/AasxSomeProject/SomeFile.cs
  ```

* Commit locally:

  ```
  git commit
  ```

  Please observe our [guideline related to commit messages][commit-messages]:
  1) First line is a subject, max. 50 characters, starts with a verb in 
     imperative mood
  2) Empty line
  3) Body, max. line width 72 characters, must not start with the first word of
     the subject

* Run the pre-commit checks and make sure they all pass:

  ```
  .\src\Check.ps1
  ```

* If needed, change your commit message:

  ```
  git commit --amend
  ```

* Set the upstream of your branch:

  ```
  git branch --set-upstream-to origin/yourUsername/Add-some-new-feature
  ```

* Push your changes:

  ```
  git push
  ```

### Pull Request
 
* Go to the [aasx-package-explorer GitHub Repository][repository-home] and
  create the pull request in the web interface.
* Have it reviewed, if necessary
* Make sure all the remote checks pass
* Sign Contributor License Agreement (CLA) on the page of your pull request.
  This needs to be done only once.
  
### Merge

**If you are a member of [admin-shell-io GitHub organization][organization]**:
 
* Squash & Merge (see 
  [this section of the GitHub documentation][squash-and-merge])  

**Otherwise**:

* Ask somebody from the organization to squash & merge the pull request for you 

## Visual Studio - Solution structure

The VS solution is structured in multiple folders

* Plugins: all plugin reside here

## Create a plugin

As copying and renaming projects in Vs does not always work perfectly, here a step-by-step guidance:

* in Plugins, add a new project as .DLL for .net & WPF
* choose wisely a new name for the project, always starting with AasxPlugin..
* location shall be below src/.
* select .net-framework = 4.6.1
* copy an appropriate existing Plugin.cs (do NOT rename it!)
* copy an appropriate existing ...Options.cs (rename it, you WILL need options)
* add reference to AasxCsharpLibrary, AasxIntegrationBase, AasxIntegrationBaseWpf, AasxPredefinedConcepts
* in the ...Options.cs, check&modify the record-logic for allowing Submodel semantic ids
* choose an appropriate Submodel semantic id to be associated with the plugin
* change the contents of the Plugin.cs (do NOT rename it!)
  * change handling of options defined in ...Options.cs file 
  * change ListActions() for the actions, your plugin shall handle
    (if an action is not listed, it will not be issued by the main application to the plug-in!)
  * change / extend ActivateAction() to handle defined actions
    * use string-comparison for action names
    * always check & typecast provided arguments
* important actions are:
  * "call-check-visual-extension": the main app calls this action for each Submodel to check, if the plug-in feels responsible
  * "get-events": retrieves stacked events FROM the plugin
  * "event-return": pushes events from the main app TO the plugin
  * "get-check-visual-extension": returns a True, if a control shall be rendered
  * "fill-panel-visual-extension": fill a provided WPF panel with content controls
* event payloads are defined in: AasxPluginInterface.cs

[visual-studio]: https://visualstudio.microsoft.com/de/vs/community/
[organization]: https://github.com/admin-shell-io
[branches-guideline]: https://admin-shell-io.github.io/aasx-package-explorer/devdoc/getting-started/development-workflow.html#pull-requests
[forking]: https://guides.github.com/activities/forking/
[commit-messages]: https://admin-shell-io.github.io/aasx-package-explorer/devdoc/getting-started/development-workflow.html#commit-messages
[repository-home]: https://github.com/admin-shell-io/aasx-package-explorer
[squash-and-merge]: https://docs.github.com/en/github/collaborating-with-issues-and-pull-requests/merging-a-pull-request
