{
  description = "DNDHelper - Avalonia/.NET port";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
  };

  outputs =
    { self, nixpkgs }:
    let
      system = "x86_64-linux";
      pkgs = nixpkgs.legacyPackages.${system};

      dotnet-sdk = pkgs.dotnetCorePackages.sdk_10_0;
      dotnet-runtime = pkgs.dotnetCorePackages.runtime_10_0;
      runtimeLibs = with pkgs; [
        gtk3
        webkitgtk_4_1
        libGL
        libxi
        libxcursor
        wayland
        libxkbcommon
      ];

    in
    {
      packages.${system}.default = pkgs.buildDotnetModule {
        pname = "DNDHelper";
        version = "13.0";

        src = ./.;
        projectFile = "DNDHelper/DNDHelper.csproj";
        nugetDeps = ./deps.json;

        inherit dotnet-sdk dotnet-runtime;
        executables = [ "DNDHelper" ];

        buildInputs = runtimeLibs;
        runtimeDeps = runtimeLibs;

        nativeBuildInputs = [ pkgs.pkg-config ];
      };

      apps.${system}.default = {
        type = "app";
        program = "${self.packages.${system}.default}/bin/DNDHelper";
      };

      devShells.${system}.default = pkgs.mkShell {
        buildInputs =
          with pkgs;
          [
            dotnet-sdk
            dotnet-runtime
            dotnetPackages.Nuget
            avalonia
          ]
          ++ runtimeLibs;

        nativeBuildInputs = [ pkgs.pkg-config ];
      };
    };
}
