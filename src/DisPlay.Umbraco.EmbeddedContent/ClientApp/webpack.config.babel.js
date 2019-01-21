import path from 'path'
import webpack from 'webpack'
import CopyWebpackPlugin from 'copy-webpack-plugin'
import MiniCssExtractPlugin from 'mini-css-extract-plugin'
import UglifyJsPlugin from 'uglifyjs-webpack-plugin'
import OptimizeCSSAssetsPlugin from 'optimize-css-assets-webpack-plugin'
import StyleLintPlugin from 'stylelint-webpack-plugin'

const pkg = require('./package.json')

const PATHS = {
  app: path.join(__dirname, 'app'),
  dist: path.join(__dirname, 'dist'),
  public: path.join(__dirname, 'public')
}
module.exports = (env, options) => {
  const isDebug = options.mode !== 'production'

  return {
    context: PATHS.app,
    entry: {
      [pkg.pluginName]: PATHS.app
    },
    output: {
      path: PATHS.dist,
      publicPath: `/App_Plugins/${pkg.pluginName}/`,
      pathinfo: isDebug,
      filename: '[name].min.js'
    },
    mode: options.mode,
    devtool: isDebug ? 'cheap-module-source-map' : 'source-map',
    bail: !isDebug,
    cache: isDebug,
    optimization: {
      runtimeChunk: false,
      splitChunks: false,
      minimizer: [
        new UglifyJsPlugin({
          cache: true,
          parallel: true,
          sourceMap: true // set to true if you want JS source maps
        }),
        new OptimizeCSSAssetsPlugin({})
      ]
    },
    devServer: {
      contentBase: PATHS.public,
      port: process.env.port || 3000,
      stats: 'errors-only',
      publicPath: `/App_Plugins/${pkg.pluginName}/`,
      proxy: {
        '*': pkg.proxy
      }
    },
    resolve: {
      modules: [PATHS.app, 'node_modules']
    },
    externals: pkg.externals,
    module: {
      rules: [
        {
          test: /.js$/,
          loader: 'babel-loader',
          include: PATHS.app,
          options: {
            envName: 'client'
          }
        },
        {
          test: /\.css$/,
          include: PATHS.app,
          use: [
            MiniCssExtractPlugin.loader,
            {
              loader: 'css-loader',
              options: {
                camelCase: true,
                importLoaders: true,
                localIdentName: '[local]',
                modules: true,
                sourceMap: true
              }
            },
            'postcss-loader'
          ]
        },
        {
          exclude: [/\.html$/, /\.js$/, /\.css$/, /\.json$/, /\.svg$/],

          use: {
            loader: 'url-loader',
            query: {
              limit: 10000,
              name: '[path][name].[ext]'
            }
          }
        },
        {
          test: /\.svg$/,

          loader: 'file-loader',
          query: {
            name: '[path][name].[ext]'
          }
        },
        {
          test: /\.html$/,

          use: [
            {
              loader: 'file-loader',
              query: {
                name: '[path][name].[ext]'
              }
            },
            {
              loader: 'extract-loader'
            },
            {
              loader: 'html-loader',
              query: {
                attrs: ['img:src', 'link:href']
              }
            }
          ]
        }
      ]
    },
    plugins: [
      ...(isDebug ? [new webpack.NamedModulesPlugin()] : []),
      new webpack.DefinePlugin({
        'process.env': {
          NODE_ENV: JSON.stringify(isDebug ? 'development' : 'production')
        }
      }),
      new StyleLintPlugin({
        files: '**/*.css'
      }),
      new MiniCssExtractPlugin({
        filename: '[name].min.css'
      }),
      new CopyWebpackPlugin([PATHS.public])
    ]
  }
}
