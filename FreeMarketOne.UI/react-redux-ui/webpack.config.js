const path = require("path");
const webpack = require("webpack");
const HtmlWebpackPlugin = require("html-webpack-plugin");

// Service plugins
const WebpackPwaManifest = require("webpack-pwa-manifest");

module.exports = env => {
  return {
    entry: path.join(__dirname, "src", "index.js"),
    output: {
      path: path.join(__dirname, "build"),
      filename: "[name].bundle.js",
      publicPath: "/"
    },
    resolve: {
      modules: [path.resolve(__dirname, "src"), "node_modules"],
      alias: {
        Assets: path.resolve(__dirname, "src", "assets"),
        Components: path.resolve(__dirname, "src", "components"),
        Layouts: path.resolve(__dirname, "src", "layouts"),
        Modules: path.resolve(__dirname, "src", "modules"),
        Router: path.resolve(__dirname, "src", "router"),
        Pages: path.resolve(__dirname, "src", "pages"),
        Styles: path.resolve(__dirname, "src", "styles"),
        "react-dom": "@hot-loader/react-dom"
      }
    },
    devServer: {
      contentBase: path.join(__dirname, "src"),
      host: "localhost",
      compress: true,
      port: 3001,
      historyApiFallback: true,
      quiet: true
    },
    devtool: "source-map",
    module: {
      rules: [
        {
          test: /\.(js|jsx)$/,
          exclude: /node_modules/,
          use: ["babel-loader"]
        },
        {
          test: /\.(css)$/,
          use: ["style-loader", "css-loader"]
        },
        {
          test: /\.(jpg|jpeg|png|gif|mp3|svg)$/,
          loaders: ["file-loader"]
        },
        {
          test: /\.(woff|woff2|eot|ttf)$/,
          loader: "url-loader"
        }
      ]
    },
    optimization: {
      splitChunks: {
        chunks: "all"
      }
    },
    plugins: [
      new HtmlWebpackPlugin({
        template: path.join(__dirname, "public", "index.html")
      }),
      new WebpackPwaManifest({
        name: "freemarketone",
        short_name: "fm1",
        description: "freemarketone",
        background_color: "#ffffff",
        theme_color: "#ffffff"
      }),

      new webpack.DefinePlugin({
        "process.env.MAIN_API_URL": JSON.stringify(env.MAIN_API_URL),
        "process.env.PANEL_API_URL": JSON.stringify(env.PANEL_API_URL)
      })
    ]
  };
};
