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
    }
};

console.log('[ChartInterop] Module loaded');
