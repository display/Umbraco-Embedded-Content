import './groups.html'

export default class GroupsController {
  constructor ($scope) {
    this.$scope = $scope

    if (!$scope.model.value) {
      $scope.model.value = []
    }
  }

  add (event) {
    event.preventDefault()

    this.hasError = false

    if (!this.newItem || this.$scope.model.value.indexOf(this.newItem) > -1) {
      this.hasError = true
      return
    }

    this.$scope.model.value.push(this.newItem)
    this.$scope.model.value.sort((a, b) => a.localeCompare(b))

    this.newItem = ''
  }

  remove (index, event) {
    event.preventDefault()
    this.$scope.model.value.splice(index, 1)
  }

  static $inject = [
    '$scope'
  ]
}
