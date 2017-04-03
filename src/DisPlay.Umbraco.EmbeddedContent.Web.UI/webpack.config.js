const path = require('path')
const webpack = require('webpack')
const pkg = require('./package.json')

const CaseSensitivePathsPlugin = require('case-sensitive-paths-webpack-plugin')
const CopyWebpackPlugin = require('copy-webpack-plugin')
const ExtractTextPlugin = require('extract-text-webpack-plugin')
const NgAnnotatePlugin = require('ng-annotate-webpack-plugin')
const StyleLintPlugin = require('stylelint-webpack-plugin')

const PATHS = {
  app: path.join(__dirname, 'app'),
  build: path.join(__dirname, 'build'),
  public: path.join(__dirname, 'public')

}

module.exports = function (env) {
  const isDebug = env !== 'production'

  return {
    context: PATHS.app,
    entry: {
      [pkg.pluginName]: PATHS.app
    },
    output: {
      path: PATHS.build,
      pathinfo: isDebug,
      publicPath: `/App_Plugins/${pkg.pluginName}/`,
      filename: '[name].min.js'
    },
    devtool: isDebug ? 'cheap-module-source-map' : 'source-map',
    bail: !isDebug,
    cache: isDebug,
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
      modules: [
        PATHS.app,
        'node_modules'
      ]
    },
    externals: pkg.externals,
    module: {
      rules: [
        {
          test: /\.js$/,
          enforce: 'pre',

          loader: 'eslint-loader',
          options: {
            emitWarning: true
          }
        },
        {
          test: /\.js$/,
          exclude: /(node_modules|bower_components)/,

          loader: 'babel-loader',
          query: {
            cacheDirectory: isDebug,
            presets: [['env', { modules: false, targets: pkg.browsers }]],
            plugins: [
              'transform-class-properties',
              'transform-object-rest-spread',
              'transform-runtime'
            ],
          }
        },
        {
          test: /\.css$/,

          use: ExtractTextPlugin.extract({
            fallback: 'style-loader',
            use: [
              {
                loader: 'css-loader',
                options: {
                  camelCase: true,
                  discardComments: { removeAll: true },
                  importLoaders: 1,
                  localIdentName: '[local]', // '[name]-[local]-[hash:base64:5]',
                  modules: 1,
                  minimize: !isDebug,
                  sourceMap: true
                }
              },
              {
                loader: 'postcss-loader'
              }
            ]
          })
        },
        {
          exclude: [
            /\.html$/,
            /\.js$/,
            /\.css$/,
            /\.json$/,
            /\.svg$/
          ],

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
      new CaseSensitivePathsPlugin(),
      new StyleLintPlugin({
        files: '**/*.css'
      }),
      new ExtractTextPlugin({
        allChunks: !isDebug,
        filename: '[name].min.css'
      }),
      new CopyWebpackPlugin([
        {
          from: PATHS.public,
          transform: (content, path) =>
            content.toString().replace(/%PLUGIN_NAME%/g, pkg.pluginName)
        }
      ]),
      ...isDebug ? [
      ] : [
        new NgAnnotatePlugin(),
        new webpack.optimize.UglifyJsPlugin({
          sourceMap: true,
          compress: {
            screw_ie8: true,
            warnings: false,
            unused: true,
            dead_code: true
          },
          mangle: {
            screw_ie8: true
          },
          output: {
            comments: false,
            screw_ie8: true
          }
        })
      ]
    ]
  }
}
