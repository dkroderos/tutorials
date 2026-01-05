SLN_FILE=./Ctf.slnx
APPHOST_PATH=./aspire/Ctf.AppHost/Ctf.AppHost.csproj
API_PATH=./src/Ctf.Api/Ctf.Api.csproj

.PHONY: default build watch run test upgrade

default:
	@echo "Available tasks:"
	@echo "  build   - Build the solution"
	@echo "  watch   - Watch and run the AppHost project"
	@echo "  run     - Run the AppHost project"
	@echo "  test    - Test the solution"
	@echo "  upgrade - Upgrade packages of all projects"

build:
	dotnet build $(SLN_FILE)

watch:
	dotnet watch --project $(APPHOST_PATH) -c Debug

run:
	dotnet run --project $(APPHOST_PATH) -c Debug

test:
	dotnet build $(SLN_FILE)
	dotnet test $(SLN_FILE)

upgrade:
	dotnet outdated --upgrade
