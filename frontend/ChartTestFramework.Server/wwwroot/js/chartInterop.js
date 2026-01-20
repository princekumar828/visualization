/**
 * Chart Interop JavaScript Module
 * Provides JS interop functions for chart libraries
 */

window.chartInterop = {
    // Store chart instances
    instances: {},

    // Performance timing helper
    timing: {
        start: function (name) {
            this[name] = performance.now();
        },
        end: function (name) {
            const elapsed = performance.now() - (this[name] || 0);
            console.log(`[ChartInterop] ${name}: ${elapsed.toFixed(2)}ms`);
            return elapsed;
        }
    },

    // Apache ECharts implementation
    echarts: {
        init: function (containerId) {
            console.log(`[ECharts] Initializing in #${containerId}`);
            const container = document.getElementById(containerId);
            if (!container) {
                throw new Error(`Container #${containerId} not found`);
            }

            // Dispose existing instance
            if (window.chartInterop.instances[containerId]) {
                window.chartInterop.instances[containerId].dispose();
            }

            // Create new instance
            const chart = echarts.init(container, 'dark');
            window.chartInterop.instances[containerId] = chart;

            // Handle resize
            window.addEventListener('resize', () => chart.resize());
        },

        renderBoxPlot: function (containerId, data, dotNetRef) {
            console.log(`[ECharts] Rendering box plot with ${data.metadata.total_points} points`);
            window.chartInterop.timing.start('echarts_render');

            const chart = window.chartInterop.instances[containerId];
            if (!chart) {
                throw new Error(`Chart instance not found for #${containerId}`);
            }

            // Build hierarchical axis data
            const xAxisData = data.lotLabels;

            const option = {
                title: {
                    text: `Semiconductor Yield - ${data.metadata.year}`,
                    subtext: `${data.metadata.total_lots} Lots | ${data.metadata.total_points} Data Points`,
                    left: 'center'
                },
                tooltip: {
                    trigger: 'item',
                    formatter: function (params) {
                        if (params.seriesType === 'boxplot') {
                            return `<strong>${params.name}</strong><br/>
                                Max: ${params.data[5]}<br/>
                                Q3: ${params.data[4]}<br/>
                                Median: ${params.data[3]}<br/>
                                Q1: ${params.data[2]}<br/>
                                Min: ${params.data[1]}`;
                        } else if (params.seriesType === 'scatter') {
                            return `<strong>${params.data.lot}</strong><br/>
                                Wafer: ${params.data.wafer}<br/>
                                Yield: ${params.data.y}%`;
                        }
                    }
                },
                toolbox: {
                    feature: {
                        brush: {
                            type: ['rect', 'clear']
                        },
                        dataZoom: {},
                        saveAsImage: {}
                    }
                },
                brush: {
                    xAxisIndex: 0,
                    yAxisIndex: 0,
                    brushMode: 'single',
                    throttleType: 'debounce',
                    throttleDelay: 300
                },
                dataZoom: [
                    {
                        type: 'slider',
                        xAxisIndex: 0,
                        start: 0,
                        end: Math.min(100, 2000 / xAxisData.length * 100)
                    },
                    {
                        type: 'inside',
                        xAxisIndex: 0
                    }
                ],
                xAxis: [
                    {
                        type: 'category',
                        data: xAxisData,
                        axisLabel: {
                            rotate: 45,
                            interval: Math.floor(xAxisData.length / 20)
                        },
                        name: 'Lot ID'
                    }
                ],
                yAxis: {
                    type: 'value',
                    name: 'Yield (%)',
                    min: 70,
                    max: 100
                },
                grid: {
                    left: '10%',
                    right: '10%',
                    bottom: '20%',
                    top: '15%'
                },
                series: [
                    {
                        name: 'Box Plot',
                        type: 'boxplot',
                        data: data.boxplotData.map((d, i) => ({
                            value: d,
                            name: xAxisData[i]
                        })),
                        itemStyle: {
                            color: '#5470c6',
                            borderColor: '#91cc75'
                        }
                    },
                    {
                        name: 'Wafers',
                        type: 'scatter',
                        data: data.scatterData,
                        symbolSize: 4,
                        itemStyle: {
                            color: 'rgba(255, 200, 100, 0.6)'
                        }
                    }
                ]
            };

            chart.setOption(option);

            // Store dotNetRef for callbacks
            window.chartInterop.instances[`${containerId}_dotnet`] = dotNetRef;

            // Setup brush event
            chart.on('brushSelected', function (params) {
                if (dotNetRef && params.batch && params.batch[0]) {
                    const selected = params.batch[0].selected[1]; // scatter series
                    if (selected && selected.dataIndex && selected.dataIndex.length > 0) {
                        const area = params.batch[0].areas[0];
                        dotNetRef.invokeMethodAsync('OnBrushSelect',
                            area.coordRange[0][0], area.coordRange[0][1],
                            area.coordRange[1][0], area.coordRange[1][1],
                            selected.dataIndex);
                    }
                }
            });

            window.chartInterop.timing.end('echarts_render');
        },

        enableBrush: function (dotNetRef) {
            // Brush is already enabled via toolbox
            console.log('[ECharts] Brush selection enabled');
        },

        clearBrush: function () {
            // Clear all brush selections
            Object.keys(window.chartInterop.instances).forEach(key => {
                if (!key.endsWith('_dotnet')) {
                    const chart = window.chartInterop.instances[key];
                    if (chart && chart.dispatchAction) {
                        chart.dispatchAction({
                            type: 'brush',
                            areas: []
                        });
                    }
                }
            });
        },

        resize: function () {
            Object.keys(window.chartInterop.instances).forEach(key => {
                if (!key.endsWith('_dotnet')) {
                    const chart = window.chartInterop.instances[key];
                    if (chart && chart.resize) {
                        chart.resize();
                    }
                }
            });
        },

        destroy: function () {
            Object.keys(window.chartInterop.instances).forEach(key => {
                if (!key.endsWith('_dotnet')) {
                    const chart = window.chartInterop.instances[key];
                    if (chart && chart.dispose) {
                        chart.dispose();
                    }
                }
                delete window.chartInterop.instances[key];
            });
        }
    },

    // Syncfusion Charts implementation
    syncfusion: {
        init: function (containerId) {
            console.log(`[Syncfusion] Initializing in #${containerId}`);
            const container = document.getElementById(containerId);
            if (!container) {
                throw new Error(`Container #${containerId} not found`);
            }
            container.innerHTML = '';
        },

        renderBoxPlot: function (containerId, data, dotNetRef) {
            console.log(`[Syncfusion] Rendering box plot with ${data.metadata.total_points} points`);
            window.chartInterop.timing.start('syncfusion_render');

            const container = document.getElementById(containerId);

            // Syncfusion box and whisker chart configuration
            const chart = new ej.charts.Chart({
                primaryXAxis: {
                    valueType: 'Category',
                    title: 'Lot ID',
                    labelRotation: 45,
                    labelIntersectAction: 'Hide'
                },
                primaryYAxis: {
                    minimum: 70,
                    maximum: 100,
                    title: 'Yield (%)',
                    interval: 5
                },
                title: `Semiconductor Yield - ${data.metadata.year}`,
                subTitle: `${data.metadata.total_lots} Lots | ${data.metadata.total_points} Data Points`,
                legendSettings: { visible: true },
                tooltip: { enable: true },
                zoomSettings: {
                    enableSelectionZooming: true,
                    enablePinchZooming: true,
                    enableScrollbar: true,
                    enablePan: true,
                    mode: 'X'
                },
                selectionMode: 'DragXY',
                selectionPattern: 'Box',
                series: [{
                    type: 'BoxAndWhisker',
                    dataSource: data.series,
                    xName: 'x',
                    yName: 'outliers',
                    name: 'Yield Distribution',
                    marker: {
                        visible: true,
                        width: 4,
                        height: 4
                    },
                    boxPlotMode: 'Normal',
                    showMean: true
                }],
                selectionComplete: function (args) {
                    if (dotNetRef && args.selectedDataValues) {
                        const minX = Math.min(...args.selectedDataValues.map(d => d.x));
                        const maxX = Math.max(...args.selectedDataValues.map(d => d.x));
                        const minY = Math.min(...args.selectedDataValues.map(d => d.y));
                        const maxY = Math.max(...args.selectedDataValues.map(d => d.y));
                        dotNetRef.invokeMethodAsync('OnRectangularSelect', minX, maxX, minY, maxY);
                    }
                }
            });

            chart.appendTo(container);
            window.chartInterop.instances[containerId] = chart;
            window.chartInterop.instances[`${containerId}_dotnet`] = dotNetRef;

            window.chartInterop.timing.end('syncfusion_render');
        },

        enableSelection: function (dotNetRef) {
            console.log('[Syncfusion] Selection already enabled');
        },

        clearSelection: function () {
            Object.keys(window.chartInterop.instances).forEach(key => {
                if (!key.endsWith('_dotnet')) {
                    const chart = window.chartInterop.instances[key];
                    if (chart && chart.clearSelection) {
                        chart.clearSelection();
                    }
                }
            });
        },

        resize: function () {
            Object.keys(window.chartInterop.instances).forEach(key => {
                if (!key.endsWith('_dotnet')) {
                    const chart = window.chartInterop.instances[key];
                    if (chart && chart.refresh) {
                        chart.refresh();
                    }
                }
            });
        },

        destroy: function () {
            Object.keys(window.chartInterop.instances).forEach(key => {
                if (!key.endsWith('_dotnet')) {
                    const chart = window.chartInterop.instances[key];
                    if (chart && chart.destroy) {
                        chart.destroy();
                    }
                }
                delete window.chartInterop.instances[key];
            });
        }
    },

    // Server-rendered image (True SSR)
    serverImage: {
        render: function (containerId, dataUri, libraryName, renderTimeMs) {
            libraryName = libraryName || 'Server';
            renderTimeMs = renderTimeMs || 0;
            console.log(`[ServerImage/${libraryName}] Rendering in #${containerId} (${renderTimeMs.toFixed(1)}ms)`);
            window.chartInterop.timing.start('server_image_render');

            const container = document.getElementById(containerId);
            if (!container) {
                throw new Error(`Container #${containerId} not found`);
            }

            // Clear container and add image
            container.innerHTML = '';
            container.style.display = 'flex';
            container.style.alignItems = 'center';
            container.style.justifyContent = 'center';
            container.style.backgroundColor = '#1a1a3e';

            const img = document.createElement('img');
            img.src = dataUri;
            img.style.maxWidth = '100%';
            img.style.maxHeight = '100%';
            img.style.objectFit = 'contain';
            img.alt = 'Server-rendered box plot';

            // Add info badge
            const badge = document.createElement('div');
            badge.style.position = 'absolute';
            badge.style.top = '10px';
            badge.style.right = '10px';
            badge.style.padding = '8px 12px';
            badge.style.backgroundColor = 'rgba(16, 185, 129, 0.9)';
            badge.style.color = 'white';
            badge.style.borderRadius = '6px';
            badge.style.fontSize = '12px';
            badge.style.fontWeight = 'bold';
            badge.style.boxShadow = '0 2px 4px rgba(0,0,0,0.2)';
            badge.style.zIndex = '10';
            badge.innerHTML = `🖼️ ${libraryName} <span style="opacity:0.8">| ${renderTimeMs.toFixed(0)}ms</span>`;

            container.appendChild(img);
            container.appendChild(badge);

            window.chartInterop.timing.end('server_image_render');
        },

        renderSvg: function (containerId, svgContent, libraryName, renderTimeMs) {
            console.log(`[ServerImage/${libraryName}] Injecting SVG in #${containerId}`);
            const container = document.getElementById(containerId);
            if (!container) return;

            container.innerHTML = svgContent;
            container.style.display = 'flex';
            container.style.alignItems = 'center';
            container.style.justifyContent = 'center';
            container.style.backgroundColor = '#1a1a3e'; // Match dark theme
            container.style.position = 'relative';

            // Center the SVG
            const svg = container.querySelector('svg');
            if (svg) {
                svg.style.maxWidth = '100%';
                svg.style.height = 'auto';
            }

            // Show badge
            const badge = document.createElement('div');
            badge.style.position = 'absolute';
            badge.style.top = '10px';
            badge.style.right = '10px';
            badge.style.padding = '8px 12px';
            badge.style.backgroundColor = 'rgba(16, 185, 129, 0.9)';
            badge.style.color = 'white';
            badge.style.borderRadius = '6px';
            badge.style.fontSize = '12px';
            badge.style.fontWeight = 'bold';
            badge.style.boxShadow = '0 2px 4px rgba(0,0,0,0.2)';
            badge.style.zIndex = '10';
            badge.innerHTML = `⚡ ${libraryName} <span style="opacity:0.8">| ${renderTimeMs.toFixed(0)}ms</span>`;

            container.appendChild(badge);


        },

        destroy: function (containerId) {
            const container = document.getElementById(containerId);
            if (container) {
                container.innerHTML = '';
            }
        }
    },

    // ECharts-GL WebGL implementation
    echartsGL: {
        init: function (containerId) {
            console.log(`[ECharts-GL] Initializing WebGL in #${containerId}`);
            const container = document.getElementById(containerId);
            if (!container) {
                throw new Error(`Container #${containerId} not found`);
            }

            if (window.chartInterop.instances[containerId]) {
                window.chartInterop.instances[containerId].dispose();
            }

            const chart = echarts.init(container, 'dark', { renderer: 'canvas' });
            window.chartInterop.instances[containerId] = chart;
            window.addEventListener('resize', () => chart.resize());
        },

        renderScatterGL: function (containerId, data, dotNetRef) {
            console.log(`[ECharts-GL] Rendering ${data.scatterData.length} points with WebGL`);
            window.chartInterop.timing.start('echarts_gl_render');

            const chart = window.chartInterop.instances[containerId];
            if (!chart) {
                throw new Error(`Chart instance not found for #${containerId}`);
            }

            const option = {
                title: {
                    text: `Semiconductor Yield - ${data.metadata.year}`,
                    subtext: `${data.totalLots} Lots | ${data.scatterData.length.toLocaleString()} Points (WebGL)`,
                    left: 'center',
                    textStyle: { color: '#fff' }
                },
                tooltip: {
                    trigger: 'item',
                    formatter: p => `${p.data.lot}<br/>Wafer: ${p.data.wafer}<br/>Yield: ${p.data.y}%`
                },
                toolbox: {
                    feature: {
                        brush: { type: ['rect', 'clear'] },
                        dataZoom: {},
                        saveAsImage: {}
                    }
                },
                brush: {
                    xAxisIndex: 0,
                    yAxisIndex: 0,
                    brushMode: 'single',
                    throttleType: 'debounce',
                    throttleDelay: 300
                },
                dataZoom: [
                    { type: 'slider', xAxisIndex: 0, start: 0, end: 100 },
                    { type: 'inside', xAxisIndex: 0 }
                ],
                xAxis: {
                    type: 'value',
                    name: 'Lot Index',
                    min: 0,
                    max: data.totalLots
                },
                yAxis: {
                    type: 'value',
                    name: 'Yield (%)',
                    min: 70,
                    max: 100
                },
                grid: {
                    left: '10%', right: '10%', bottom: '15%', top: '15%'
                },
                series: [{
                    name: 'Wafers',
                    type: 'scatterGL',  // WebGL scatter
                    data: data.scatterData.map(d => ({ value: [d.x, d.y], lot: d.lot, wafer: d.wafer })),
                    symbolSize: 3,
                    itemStyle: {
                        color: new echarts.graphic.RadialGradient(0.5, 0.5, 0.5, [
                            { offset: 0, color: 'rgba(255, 200, 100, 1)' },
                            { offset: 1, color: 'rgba(255, 150, 50, 0.6)' }
                        ])
                    }
                }]
            };

            chart.setOption(option);
            window.chartInterop.instances[`${containerId}_dotnet`] = dotNetRef;

            chart.on('brushSelected', function (params) {
                if (dotNetRef && params.batch && params.batch[0]) {
                    const selected = params.batch[0].selected[0];
                    if (selected && selected.dataIndex && selected.dataIndex.length > 0) {
                        const area = params.batch[0].areas[0];
                        dotNetRef.invokeMethodAsync('OnBrushSelect',
                            area.coordRange[0][0], area.coordRange[0][1],
                            area.coordRange[1][0], area.coordRange[1][1],
                            selected.dataIndex);
                    }
                }
            });

            window.chartInterop.timing.end('echarts_gl_render');
        },

        enableBrush: function (dotNetRef) {
            console.log('[ECharts-GL] Brush selection enabled');
        },

        clearBrush: function () {
            Object.keys(window.chartInterop.instances).forEach(key => {
                if (!key.endsWith('_dotnet')) {
                    const chart = window.chartInterop.instances[key];
                    if (chart && chart.dispatchAction) {
                        chart.dispatchAction({ type: 'brush', areas: [] });
                    }
                }
            });
        },

        resize: function () {
            window.chartInterop.echarts.resize();
        },

        destroy: function () {
            window.chartInterop.echarts.destroy();
        }
    },

    // Deck.gl WebGL implementation
    deckgl: {
        deck: null,

        init: function (containerId) {
            console.log(`[Deck.gl] Initializing WebGL in #${containerId}`);
            const container = document.getElementById(containerId);
            if (!container) {
                throw new Error(`Container #${containerId} not found`);
            }
            container.innerHTML = '';
            container.style.position = 'relative';
        },

        renderScatter: function (containerId, data, dotNetRef) {
            console.log(`[Deck.gl] Rendering ${data.points.length} points with WebGL GPU`);
            window.chartInterop.timing.start('deckgl_render');

            const container = document.getElementById(containerId);
            const width = container.clientWidth;
            const height = container.clientHeight;

            // Normalize coordinates
            const xScale = (x) => (x / data.totalLots) * width;
            const yScale = (y) => height - ((y - data.yMin) / (data.yMax - data.yMin)) * height;

            const scatterLayer = new deck.ScatterplotLayer({
                id: 'scatter-layer',
                data: data.points,
                getPosition: d => [xScale(d.position[0]), yScale(d.position[1])],
                getRadius: 3,
                getFillColor: d => {
                    const normalized = (d.yield - 70) / 30;
                    return [255, Math.floor(100 + normalized * 155), 50, 200];
                },
                pickable: true,
                onClick: info => {
                    if (info.object) {
                        console.log(`Clicked: ${info.object.lot} - ${info.object.wafer}: ${info.object.yield}%`);
                    }
                }
            });

            if (this.deck) {
                this.deck.finalize();
            }

            this.deck = new deck.Deck({
                parent: container,
                width: width,
                height: height,
                views: new deck.OrthographicView(),
                initialViewState: {
                    target: [width / 2, height / 2, 0],
                    zoom: 0
                },
                controller: true,
                layers: [scatterLayer],
                getTooltip: ({ object }) => object && `${object.lot}\n${object.wafer}: ${object.yield}%`
            });

            // Add title overlay
            const titleDiv = document.createElement('div');
            titleDiv.style.cssText = 'position:absolute;top:10px;left:10px;color:white;font-size:16px;font-weight:bold;text-shadow:0 2px 4px rgba(0,0,0,0.5);';
            titleDiv.innerHTML = `Semiconductor Yield - ${data.metadata.year}<br/><span style="font-size:12px;opacity:0.8">${data.points.length.toLocaleString()} Points | Deck.gl WebGL GPU</span>`;
            container.appendChild(titleDiv);

            // Add badge
            const badge = document.createElement('div');
            badge.style.cssText = 'position:absolute;top:10px;right:10px;padding:8px 12px;background:rgba(59,130,246,0.9);color:white;border-radius:6px;font-size:12px;font-weight:bold;';
            badge.textContent = '🚀 GPU Accelerated';
            container.appendChild(badge);

            window.chartInterop.instances[containerId] = this.deck;
            window.chartInterop.instances[`${containerId}_dotnet`] = dotNetRef;

            window.chartInterop.timing.end('deckgl_render');
        },

        enableSelection: function (dotNetRef) {
            console.log('[Deck.gl] Selection mode enabled');
        },

        clearSelection: function () {
            // Deck.gl selection cleared
        },

        resize: function () {
            if (this.deck) {
                this.deck.redraw(true);
            }
        },

        destroy: function () {
            if (this.deck) {
                this.deck.finalize();
                this.deck = null;
            }
        }
    }
};

console.log('[ChartInterop] Module loaded with ECharts-GL and Deck.gl support');

